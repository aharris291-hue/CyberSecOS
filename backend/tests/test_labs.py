"""Graded labs: reading a lab, marking answers, and how a pass feeds lesson progress."""

import pytest

from app.models import Lab, LabAttempt, Lesson
from app.services import lab as lab_service

SLUG = "linux-auth-logs"


def _lesson(db, slug=SLUG):
    return db.query(Lesson).filter_by(slug=slug).one()


def _answers(lesson, right=None):
    """Correct answers for every task, or for the first ``right`` tasks and blanks after."""
    tasks = lesson.lab.tasks
    right = len(tasks) if right is None else right
    return [task["accepted"][0] if i < right else None for i, task in enumerate(tasks)]


def _submit(client, headers, lesson, answers):
    return client.post(f"/api/lessons/{lesson.id}/lab/attempts", headers=headers, json={"answers": answers})


# --- Marking ----------------------------------------------------------------------


@pytest.mark.parametrize(
    ("task", "answer", "expected"),
    [
        ({"type": "text", "accepted": ["svc_backup"]}, "  SVC_Backup. ", True),
        ({"type": "text", "accepted": ["svc_backup"]}, "'svc_backup'", True),
        ({"type": "text", "accepted": ["svc_backup"]}, "svc_backups", False),
        ({"type": "text", "accepted": ["cat /etc/shadow"]}, "cat   /etc/shadow", True),
        ({"type": "number", "accepted": ["1204"]}, "1,204", True),
        ({"type": "number", "accepted": ["16"]}, "16.0", True),
        ({"type": "number", "accepted": ["16"]}, "sixteen", False),
        ({"type": "number", "accepted": ["300"], "tolerance": 5}, "296", True),
        ({"type": "number", "accepted": ["300"], "tolerance": 5}, "306", False),
        ({"type": "choice", "accepted": ["2"]}, "2", True),
        ({"type": "choice", "accepted": ["2"]}, "1", False),
        ({"type": "set", "accepted": ["22, 80, 443"]}, "443 80\n22", True),
        ({"type": "set", "accepted": ["22, 80, 443"]}, "22;80;443;", True),
        ({"type": "set", "accepted": ["22, 80, 443"]}, "22, 80", False),
        ({"type": "set", "accepted": ["22, 80, 443"]}, "22, 80, 443, 8080", False),
        ({"type": "set", "accepted": ["FS-01, SQL-02"]}, "sql-02, fs-01", True),
        ({"type": "text", "accepted": ["x"]}, "   ", False),
        ({"type": "text", "accepted": ["x"]}, None, False),
    ],
)
def test_answers_are_normalised_before_comparison(task, answer, expected):
    assert lab_service.is_correct(task, answer) is expected


def test_grade_counts_score_and_hides_explanations_until_passed():
    tasks = [
        {"type": "text", "accepted": [str(i)], "hint": f"hint {i}", "explanation": f"why {i}"} for i in range(5)
    ]
    failed = lab_service.grade(tasks, ["0", "1", "nope", None, "4"], pass_mark=80)
    assert (failed.score, failed.correct, failed.total, failed.passed) == (60, 3, 5, False)
    assert [r.feedback for r in failed.results] == ["why 0", "why 1", "hint 2", "hint 3", "why 4"]

    passed = lab_service.grade(tasks, ["0", "1", "2", "3", "x"], pass_mark=80)
    assert passed.passed and passed.score == 80
    assert passed.results[4].feedback == "why 4", "a passed lab explains every task, including misses"


def test_grade_rejects_the_wrong_number_of_answers():
    with pytest.raises(ValueError):
        lab_service.grade([{"type": "text", "accepted": ["a"], "hint": "h", "explanation": "e"}], [])


# --- Seeding ----------------------------------------------------------------------


def test_every_course_gets_a_lab_attached_to_a_lesson(db):
    labs = db.query(Lab).all()
    assert len(labs) == 12
    assert len({lab.lesson.course_id for lab in labs}) == 12


def test_reseeding_labs_is_idempotent_and_keeps_attempts(client, auth_headers, db):
    from app.services.seed import seed_database

    lesson = _lesson(db)
    _submit(client, auth_headers, lesson, _answers(lesson))
    lab_id = lesson.lab.id
    seed_database(db)
    assert db.query(Lab).count() == 12
    assert _lesson(db).lab.id == lab_id
    assert db.query(LabAttempt).count() == 1


# --- API --------------------------------------------------------------------------


def test_lab_detail_shows_tasks_and_data_but_never_the_answer_key(client, auth_headers, db):
    lesson = _lesson(db)
    body = client.get(f"/api/lessons/{lesson.id}/lab", headers=auth_headers).json()
    assert body["title"] and body["scenario"] and body["pass_mark"] == 80
    assert body["artifacts"][0]["name"] == "auth.log" and "Failed password" in body["artifacts"][0]["content"]
    assert [t["index"] for t in body["tasks"]] == list(range(len(lesson.lab.tasks)))
    assert body["status"] == {"attempts": 0, "best_score": None, "passed": False}
    raw = str(body)
    assert "accepted" not in raw and "explanation" not in raw and "hint" not in raw
    choice = next(t for t in body["tasks"] if t["type"] == "choice")
    assert len(choice["options"]) >= 3


def test_lesson_detail_flags_a_graded_lab(client, auth_headers, db):
    assert client.get(f"/api/lessons/{_lesson(db).id}", headers=auth_headers).json()["has_graded_lab"] is True
    plain = _lesson(db, "net-osi-tcpip")
    assert client.get(f"/api/lessons/{plain.id}", headers=auth_headers).json()["has_graded_lab"] is False
    assert client.get(f"/api/lessons/{plain.id}/lab", headers=auth_headers).status_code == 404


def test_artifacts_download_as_text_files(client, auth_headers, db):
    lesson = _lesson(db)
    response = client.get(f"/api/lessons/{lesson.id}/lab/artifacts/auth.log", headers=auth_headers)
    assert response.status_code == 200
    assert response.headers["content-type"].startswith("text/plain")
    assert 'filename="auth.log"' in response.headers["content-disposition"]
    assert response.text == lesson.lab.artifacts[0]["content"]
    missing = client.get(f"/api/lessons/{lesson.id}/lab/artifacts/nope.txt", headers=auth_headers)
    assert missing.status_code == 404


def test_lab_endpoints_need_a_login(client, db):
    lesson = _lesson(db)
    assert client.get(f"/api/lessons/{lesson.id}/lab").status_code == 401
    assert client.post(f"/api/lessons/{lesson.id}/lab/attempts", json={"answers": []}).status_code == 401
    assert client.get("/api/labs").status_code == 401


def test_a_failed_attempt_gives_hints_and_does_not_complete_the_lab(client, auth_headers, db):
    lesson = _lesson(db)
    body = _submit(client, auth_headers, lesson, _answers(lesson, right=2)).json()
    assert body["passed"] is False and body["correct"] == 2 and body["lab_completed"] is False
    assert body["lesson_status"] == "in_progress"
    tasks = lesson.lab.tasks
    assert body["results"][0]["feedback"] == tasks[0]["explanation"]
    assert body["results"][3]["feedback"] == tasks[3]["hint"]
    progress = client.get(f"/api/lessons/{lesson.id}", headers=auth_headers).json()["progress"]
    assert progress["lab_completed"] is False


def test_passing_the_lab_completes_it_once_and_logs_lab_time(client, auth_headers, db):
    lesson = _lesson(db)
    body = _submit(client, auth_headers, lesson, _answers(lesson)).json()
    assert body["passed"] and body["score"] == 100 and body["lab_completed"]
    assert all(r["is_correct"] for r in body["results"])

    _submit(client, auth_headers, lesson, _answers(lesson))  # a second pass is recorded but logs no more time
    totals = client.get("/api/progress", headers=auth_headers).json()["totals"]
    assert totals["labs_completed"] == 1 and totals["lab_minutes"] == lesson.lab_minutes

    status = client.get(f"/api/lessons/{lesson.id}/lab", headers=auth_headers).json()["status"]
    assert status == {"attempts": 2, "best_score": 100, "passed": True}


def test_passing_lab_and_quiz_completes_the_lesson(client, auth_headers, db):
    lesson = _lesson(db)
    client.post(
        f"/api/lessons/{lesson.id}/quiz",
        headers=auth_headers,
        json={"answers": [q.correct_index for q in lesson.questions]},
    )
    body = _submit(client, auth_headers, lesson, _answers(lesson)).json()
    assert body["lesson_status"] == "completed"


def test_a_graded_lab_cannot_be_self_marked_complete(client, auth_headers, db):
    lesson = _lesson(db)
    response = client.post(f"/api/lessons/{lesson.id}/lab", headers=auth_headers, json={"notes": "done"})
    assert response.status_code == 409
    # Once passed, the learner can still save notes against it.
    _submit(client, auth_headers, lesson, _answers(lesson))
    response = client.post(f"/api/lessons/{lesson.id}/lab", headers=auth_headers, json={"notes": "Blocked the IP"})
    assert response.status_code == 200 and response.json()["lab_notes"] == "Blocked the IP"


def test_submission_must_answer_each_task_and_stay_short(client, auth_headers, db):
    lesson = _lesson(db)
    assert _submit(client, auth_headers, lesson, ["x"]).status_code == 422
    too_long = ["x" * (lab_service.MAX_ANSWER_LENGTH + 1)] + [None] * (len(lesson.lab.tasks) - 1)
    assert _submit(client, auth_headers, lesson, too_long).status_code == 422
    assert db.query(LabAttempt).count() == 0


def test_lab_list_covers_every_course_in_order_with_status(client, auth_headers, db):
    lesson = _lesson(db)
    _submit(client, auth_headers, lesson, _answers(lesson, right=1))
    labs = client.get("/api/labs", headers=auth_headers).json()
    assert len(labs) == 12
    assert labs[0]["course_slug"] == "networking-foundations"
    mine = next(lab for lab in labs if lab["lesson_id"] == lesson.id)
    assert mine["status"]["attempts"] == 1 and mine["status"]["passed"] is False
    assert mine["task_count"] == len(lesson.lab.tasks)
    others = [lab for lab in labs if lab["lesson_id"] != lesson.id]
    assert all(lab["status"]["attempts"] == 0 for lab in others)


def test_attempts_are_per_learner(client, auth_headers, db):
    lesson = _lesson(db)
    _submit(client, auth_headers, lesson, _answers(lesson))
    from app.models import User
    from app.security import create_access_token

    other = User(username="sam", password_hash="x", display_name="Sam")
    db.add(other)
    db.commit()
    token, _ = create_access_token(other, client.app.state.settings)
    other_headers = {"Authorization": f"Bearer {token}"}
    status = client.get(f"/api/lessons/{lesson.id}/lab", headers=other_headers).json()["status"]
    assert status["attempts"] == 0
