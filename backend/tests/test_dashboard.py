from app.models import Lesson
from app.services.seed import load_course_files


def test_dashboard_requires_login(client):
    assert client.get("/api/dashboard").status_code == 401


def test_new_learner_dashboard(client, auth_headers):
    body = client.get("/api/dashboard", headers=auth_headers).json()
    assert body["display_name"] == "Adam" and body["target_role"] == "SOC Analyst"
    assert body["totals"]["completed_lessons"] == 0
    assert len(body["daily_activity"]) == 14
    assert len(body["courses"]) == len(load_course_files())
    assert [c["exam_code"] for c in body["certifications"]] == ["SY0-701", "SC-900", "SC-200", "CS0-003"]
    assert body["next_lesson"]["course_title"] == "Networking Foundations for SOC Analysts"
    assert body["recent_activity"] == []


def test_dashboard_reflects_study(client, auth_headers, db):
    lesson = db.query(Lesson).filter_by(slug="net-osi-tcpip").one()
    client.post(f"/api/lessons/{lesson.id}/theory", headers=auth_headers)
    client.post(f"/api/lessons/{lesson.id}/lab", headers=auth_headers, json={})
    answers = [q.correct_index for q in lesson.questions]
    client.post(f"/api/lessons/{lesson.id}/quiz", headers=auth_headers, json={"answers": answers})

    body = client.get("/api/dashboard", headers=auth_headers).json()

    assert body["totals"]["completed_lessons"] == 1
    assert body["totals"]["current_streak_days"] == 1
    assert body["daily_activity"][-1]["lab_minutes"] == lesson.lab_minutes
    assert body["week"]["minutes"] > 0
    lessons_in_first_course = len(lesson.course.lessons)
    assert body["courses"][0]["percent"] == round(100 / lessons_in_first_course)
    assert len(body["recent_activity"]) == 3


def test_dashboard_day_range_is_bounded(client, auth_headers):
    assert client.get("/api/dashboard?days=3", headers=auth_headers).status_code == 422
    assert len(client.get("/api/dashboard?days=30", headers=auth_headers).json()["daily_activity"]) == 30
