from app.models import Lesson, QuizAttempt


def _lesson(db, slug="net-osi-tcpip"):
    return db.query(Lesson).filter_by(slug=slug).one()


def test_content_requires_login(client):
    assert client.get("/api/courses").status_code == 401


def test_list_courses_in_order(client, auth_headers):
    courses = client.get("/api/courses", headers=auth_headers).json()
    assert courses[0]["domain"] == "Networking"
    assert [c["order"] for c in courses] == sorted(c["order"] for c in courses)
    assert all(c["lesson_count"] >= 1 and c["completed_lessons"] == 0 for c in courses)


def test_course_detail_lists_lessons(client, auth_headers):
    course = client.get("/api/courses/networking-foundations", headers=auth_headers).json()
    assert [l["slug"] for l in course["lessons"]][:2] == ["net-osi-tcpip", "net-ports-protocols"]
    assert course["lessons"][0]["status"] == "not_started"


def test_unknown_course_is_404(client, auth_headers):
    assert client.get("/api/courses/nope", headers=auth_headers).status_code == 404


def test_lesson_detail_has_every_section_and_hides_answers(client, auth_headers, db):
    lesson = _lesson(db)
    body = client.get(f"/api/lessons/{lesson.id}", headers=auth_headers).json()
    for field in ("learning_objectives", "theory", "examples", "exercise", "expected_outcome", "questions"):
        assert body[field], field
    assert body["exercise"]["steps"]
    assert all("correct_index" not in q and "explanation" not in q for q in body["questions"])
    assert {o["certification_id"] for o in body["certification_objectives"]} >= {"secplus"}
    assert body["previous_lesson_id"] is None
    assert body["next_lesson_id"] == _lesson(db, "net-ports-protocols").id


def test_unknown_lesson_is_404(client, auth_headers):
    assert client.get("/api/lessons/99999", headers=auth_headers).status_code == 404


def test_certifications_listed_by_priority(client, auth_headers):
    certs = client.get("/api/certifications", headers=auth_headers).json()
    assert [c["id"] for c in certs] == ["secplus", "sc900", "sc200", "cysa"]
    assert len(certs[0]["objectives"]) == 28


def test_submit_quiz_grades_and_records_attempt(client, auth_headers, db):
    lesson = _lesson(db)
    correct = [q.correct_index for q in lesson.questions]
    response = client.post(f"/api/lessons/{lesson.id}/quiz", headers=auth_headers, json={"answers": correct})
    assert response.status_code == 200
    body = response.json()
    assert body["score"] == 100 and body["passed"] and body["pass_mark"] == 70
    assert all(r["is_correct"] and r["explanation"] for r in body["results"])
    assert db.query(QuizAttempt).filter_by(lesson_id=lesson.id).count() == 1


def test_failed_quiz_reveals_correct_answers(client, auth_headers, db):
    lesson = _lesson(db)
    wrong = [(q.correct_index + 1) % len(q.options) for q in lesson.questions]
    body = client.post(f"/api/lessons/{lesson.id}/quiz", headers=auth_headers, json={"answers": wrong}).json()
    assert body["score"] == 0 and not body["passed"]
    assert [r["correct_index"] for r in body["results"]] == [q.correct_index for q in lesson.questions]


def test_quiz_with_wrong_answer_count_is_rejected(client, auth_headers, db):
    lesson = _lesson(db)
    response = client.post(f"/api/lessons/{lesson.id}/quiz", headers=auth_headers, json={"answers": [0]})
    assert response.status_code == 422
