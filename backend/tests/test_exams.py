import random

import pytest

from app.models import ExamAttempt, Lesson
from app.services import exam as exam_service
from app.services.exam import ObjectiveBreakdown, build_exam, certification_questions, grade_exam


def test_exam_info_reports_available_questions(client, auth_headers):
    body = client.get("/api/certifications/secplus/exam", headers=auth_headers).json()
    assert body["exam_code"] == "SY0-701"
    assert body["pass_mark"] == 80
    assert body["default_size"] == 20
    assert body["available_questions"] >= 20


def test_exam_requires_login(client):
    assert client.get("/api/certifications/secplus/exam").status_code == 401


def test_unknown_certification_is_404(client, auth_headers):
    assert client.get("/api/certifications/nope/exam", headers=auth_headers).status_code == 404
    assert client.get("/api/certifications/nope/exam/paper", headers=auth_headers).status_code == 404


def test_paper_returns_requested_size_without_answers(client, auth_headers):
    body = client.get("/api/certifications/secplus/exam/paper?size=10", headers=auth_headers).json()
    assert len(body["questions"]) == 10
    assert all("correct_index" not in q and "explanation" not in q for q in body["questions"])
    assert len({q["id"] for q in body["questions"]}) == 10  # no duplicates


def test_paper_size_is_bounded(client, auth_headers):
    assert client.get("/api/certifications/secplus/exam/paper?size=1", headers=auth_headers).status_code == 422
    assert client.get("/api/certifications/secplus/exam/paper?size=999", headers=auth_headers).status_code == 422


def test_paper_caps_at_available_questions(client, auth_headers):
    info = client.get("/api/certifications/sc900/exam", headers=auth_headers).json()
    body = client.get("/api/certifications/sc900/exam/paper?size=100", headers=auth_headers).json()
    assert len(body["questions"]) == info["available_questions"]


def test_build_exam_spreads_across_objectives(db):
    """A short Security+ exam should touch several distinct objectives, not one lesson."""
    questions = build_exam(db, "secplus", 12, rng=random.Random(1))
    qo = certification_questions(db, "secplus")
    codes = {o.code for q in questions for o in qo[q.id]}
    assert len(questions) == 12
    assert len(codes) >= 5


def test_build_exam_is_deterministic_with_seed(db):
    a = [q.id for q in build_exam(db, "secplus", 15, rng=random.Random(42))]
    b = [q.id for q in build_exam(db, "secplus", 15, rng=random.Random(42))]
    assert a == b


def _answer_all(db, client, auth_headers, cert, size, correct):
    paper = client.get(f"/api/certifications/{cert}/exam/paper?size={size}", headers=auth_headers).json()
    lookup = {q.id: q for q in db.query(__import__("app.models", fromlist=["QuizQuestion"]).QuizQuestion).all()}
    answers = []
    for q in paper["questions"]:
        idx = lookup[q["id"]].correct_index
        if not correct:
            idx = (idx + 1) % len(q["options"])
        answers.append({"question_id": q["id"], "selected_index": idx})
    return answers


def test_submit_all_correct_passes_and_records(client, auth_headers, db):
    answers = _answer_all(db, client, auth_headers, "secplus", 10, correct=True)
    body = client.post(
        "/api/certifications/secplus/exam", headers=auth_headers, json={"answers": answers, "duration_seconds": 300}
    ).json()
    assert body["score"] == 100 and body["passed"] and body["pass_mark"] == 80
    assert body["total"] == 10
    assert body["breakdown"] and all(o["total"] > 0 for o in body["breakdown"])
    assert db.query(ExamAttempt).count() == 1
    assert db.query(ExamAttempt).one().duration_seconds == 300


def test_submit_all_wrong_fails_and_breaks_down_by_objective(client, auth_headers, db):
    answers = _answer_all(db, client, auth_headers, "secplus", 10, correct=False)
    body = client.post("/api/certifications/secplus/exam", headers=auth_headers, json={"answers": answers}).json()
    assert body["score"] == 0 and not body["passed"]
    assert all(o["correct"] == 0 for o in body["breakdown"])
    assert all(0 <= o["percent"] <= 100 for o in body["breakdown"])


def test_pass_mark_boundary():
    # 80% passes, 79% does not.
    from app.services.exam import PASS_MARK
    assert PASS_MARK == 80


def test_grade_exam_breakdown_counts(db):
    from app.models import QuizQuestion
    questions = build_exam(db, "secplus", 6, rng=random.Random(3))
    # Answer the first correctly, the rest wrong.
    answers = {}
    for i, q in enumerate(questions):
        full = db.get(QuizQuestion, q.id)
        answers[q.id] = full.correct_index if i == 0 else (full.correct_index + 1) % len(full.options)
    result = grade_exam(db, "secplus", answers)
    assert result.correct == 1
    assert result.total == 6
    assert sum(b.correct for b in result.breakdown) == 1


def test_duplicate_question_rejected(client, auth_headers, db):
    answers = _answer_all(db, client, auth_headers, "secplus", 5, correct=True)
    answers.append(answers[0])
    assert client.post("/api/certifications/secplus/exam", headers=auth_headers, json={"answers": answers}).status_code == 422


def test_history_returns_attempts_newest_first(client, auth_headers, db):
    for _ in range(2):
        answers = _answer_all(db, client, auth_headers, "secplus", 8, correct=True)
        client.post("/api/certifications/secplus/exam", headers=auth_headers, json={"answers": answers})
    history = client.get("/api/certifications/secplus/exam/history", headers=auth_headers).json()
    assert len(history) == 2
    assert all(a["certification_id"] == "secplus" and a["passed"] for a in history)


def test_objective_breakdown_percent():
    assert ObjectiveBreakdown("1.1", "t", 3, 4).percent == 75
    assert ObjectiveBreakdown("1.1", "t", 0, 0).percent == 0
