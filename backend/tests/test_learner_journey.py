"""End to end: a new learner signs up, studies one lesson, and sees it everywhere."""

from .conftest import PASSWORD


def test_first_lesson_journey(client):
    assert client.get("/api/auth/status").json()["registered"] is False
    client.post("/api/auth/register", json={"username": "adam", "password": PASSWORD, "display_name": "Adam"})

    token = client.post("/api/auth/login", json={"username": "adam", "password": PASSWORD}).json()["access_token"]
    headers = {"Authorization": f"Bearer {token}"}

    dashboard = client.get("/api/dashboard", headers=headers).json()
    lesson_id = dashboard["next_lesson"]["id"]

    lesson = client.get(f"/api/lessons/{lesson_id}", headers=headers).json()
    assert lesson["progress"]["status"] == "not_started"

    client.post(f"/api/lessons/{lesson_id}/theory", headers=headers)
    client.post(f"/api/lessons/{lesson_id}/lab", headers=headers, json={"notes": "Captured the handshake", "minutes": 50})

    # Answer the quiz using the explanations returned by a first, failed attempt.
    blank = [None] * len(lesson["questions"])
    first = client.post(f"/api/lessons/{lesson_id}/quiz", headers=headers, json={"answers": blank}).json()
    assert first["passed"] is False
    second = client.post(
        f"/api/lessons/{lesson_id}/quiz", headers=headers, json={"answers": [r["correct_index"] for r in first["results"]]}
    ).json()
    assert second["passed"] and second["lesson_status"] == "completed"

    dashboard = client.get("/api/dashboard", headers=headers).json()
    assert dashboard["totals"]["completed_lessons"] == 1
    assert dashboard["totals"]["quiz_attempts"] == 2
    assert dashboard["totals"]["lab_minutes"] == 50
    assert dashboard["next_lesson"]["id"] != lesson_id
    assert any(c["objectives_mastered"] > 0 for c in dashboard["certifications"])

    course = client.get(f"/api/courses/{lesson['course_slug']}", headers=headers).json()
    assert course["completed_lessons"] == 1
    assert client.get(f"/api/lessons/{lesson_id}", headers=headers).json()["progress"]["lab_notes"] == "Captured the handshake"
