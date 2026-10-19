from datetime import date, datetime, timedelta

from app.database import utcnow
from app.models import ActivityLog, Lesson, User
from app.services.stats import ProgressStats, local_date, streaks


def _lesson(db, slug="net-osi-tcpip"):
    return db.query(Lesson).filter_by(slug=slug).one()


def _pass_quiz(client, headers, lesson):
    answers = [q.correct_index for q in lesson.questions]
    return client.post(f"/api/lessons/{lesson.id}/quiz", headers=headers, json={"answers": answers}).json()


def _complete(client, headers, lesson, notes="done"):
    client.post(f"/api/lessons/{lesson.id}/theory", headers=headers)
    client.post(f"/api/lessons/{lesson.id}/lab", headers=headers, json={"notes": notes})
    return _pass_quiz(client, headers, lesson)


# --- Lesson state ------------------------------------------------------------


def test_theory_marks_lesson_in_progress(client, auth_headers, db):
    lesson = _lesson(db)
    body = client.post(f"/api/lessons/{lesson.id}/theory", headers=auth_headers).json()
    assert body["status"] == "in_progress" and body["theory_completed"]


def test_theory_is_idempotent_and_logs_time_once(client, auth_headers, db):
    lesson = _lesson(db)
    client.post(f"/api/lessons/{lesson.id}/theory", headers=auth_headers)
    client.post(f"/api/lessons/{lesson.id}/theory", headers=auth_headers)
    entries = db.query(ActivityLog).filter_by(kind="theory").all()
    assert len(entries) == 1 and entries[0].minutes == lesson.theory_minutes


def test_lab_records_notes_and_custom_minutes(client, auth_headers, db):
    lesson = _lesson(db)
    body = client.post(
        f"/api/lessons/{lesson.id}/lab", headers=auth_headers, json={"notes": "Handshake at packets 1-3", "minutes": 75}
    ).json()
    assert body["lab_completed"] and body["lab_notes"] == "Handshake at packets 1-3"
    assert db.query(ActivityLog).filter_by(kind="lab").one().minutes == 75


def test_lab_minutes_are_bounded(client, auth_headers, db):
    lesson = _lesson(db)
    response = client.post(f"/api/lessons/{lesson.id}/lab", headers=auth_headers, json={"minutes": 0})
    assert response.status_code == 422


def test_lesson_completes_only_with_lab_and_passed_quiz(client, auth_headers, db):
    lesson = _lesson(db)
    assert _pass_quiz(client, auth_headers, lesson)["lesson_status"] == "in_progress"
    body = client.post(f"/api/lessons/{lesson.id}/lab", headers=auth_headers, json={}).json()
    assert body["status"] == "completed" and body["completed_at"]


def test_failed_quiz_does_not_complete_lesson(client, auth_headers, db):
    lesson = _lesson(db)
    client.post(f"/api/lessons/{lesson.id}/lab", headers=auth_headers, json={})
    wrong = [(q.correct_index + 1) % len(q.options) for q in lesson.questions]
    body = client.post(f"/api/lessons/{lesson.id}/quiz", headers=auth_headers, json={"answers": wrong}).json()
    assert body["lesson_status"] == "in_progress"


def test_best_quiz_score_is_kept(client, auth_headers, db):
    lesson = _lesson(db)
    _pass_quiz(client, auth_headers, lesson)
    wrong = [(q.correct_index + 1) % len(q.options) for q in lesson.questions]
    client.post(f"/api/lessons/{lesson.id}/quiz", headers=auth_headers, json={"answers": wrong})
    detail = client.get(f"/api/lessons/{lesson.id}", headers=auth_headers).json()
    assert detail["progress"]["best_quiz_score"] == 100 and detail["progress"]["quiz_passed"]


def test_reopening_lab_uncompletes_lesson(client, auth_headers, db):
    lesson = _lesson(db)
    _complete(client, auth_headers, lesson)
    body = client.delete(f"/api/lessons/{lesson.id}/lab", headers=auth_headers).json()
    assert body["status"] == "in_progress" and not body["lab_completed"]


def test_course_list_counts_completed_lessons(client, auth_headers, db):
    _complete(client, auth_headers, _lesson(db))
    courses = client.get("/api/courses", headers=auth_headers).json()
    assert courses[0]["completed_lessons"] == 1
    detail = client.get("/api/courses/networking-foundations", headers=auth_headers).json()
    assert detail["lessons"][0]["status"] == "completed"


def test_progress_is_per_user(client, auth_headers, db):
    """Progress rows belong to the authenticated learner only."""
    _complete(client, auth_headers, _lesson(db))
    other = User(username="other", password_hash="x", display_name="O")
    db.add(other)
    db.commit()
    stats = ProgressStats(db, other)
    assert stats.totals()["completed_lessons"] == 0


# --- Summary -----------------------------------------------------------------


def test_empty_summary(client, auth_headers):
    body = client.get("/api/progress", headers=auth_headers).json()
    totals = body["totals"]
    assert totals["completed_lessons"] == 0 and totals["completion_percent"] == 0
    assert totals["average_quiz_score"] is None and totals["hands_on_percent"] is None
    assert body["week"]["goal_minutes"] == 600
    assert body["next_lesson"]["title"].startswith("The OSI")
    assert all(c["readiness_percent"] == 0 for c in body["certifications"])


def test_summary_after_completing_a_lesson(client, auth_headers, db):
    lesson = _lesson(db)
    _complete(client, auth_headers, lesson)
    body = client.get("/api/progress", headers=auth_headers).json()
    totals = body["totals"]
    assert totals["completed_lessons"] == 1
    assert totals["labs_completed"] == 1 and totals["quizzes_passed"] == 1
    assert totals["average_quiz_score"] == 100
    assert totals["theory_minutes"] == lesson.theory_minutes and totals["lab_minutes"] == lesson.lab_minutes
    assert totals["hands_on_percent"] >= 70
    assert totals["current_streak_days"] == 1
    assert body["week"]["minutes"] == totals["total_minutes"]
    assert body["courses"][0]["completed_lessons"] == 1
    assert body["recent_activity"][0]["kind"] == "quiz"
    assert body["recent_activity"][0]["lesson_title"] == lesson.title
    assert body["next_lesson"]["title"] == "Ports, Services and DNS"


def test_next_lesson_prefers_unfinished_work(client, auth_headers, db):
    later = _lesson(db, "linux-auth-logs")
    client.post(f"/api/lessons/{later.id}/theory", headers=auth_headers)
    body = client.get("/api/progress", headers=auth_headers).json()
    assert body["next_lesson"]["id"] == later.id and body["next_lesson"]["status"] == "in_progress"


def test_certification_readiness_counts_objectives_of_completed_lessons(client, auth_headers, db):
    lesson = _lesson(db)  # maps to secplus:4.9 and cysa:1.1
    _complete(client, auth_headers, lesson)
    certs = {c["id"]: c for c in client.get("/api/progress", headers=auth_headers).json()["certifications"]}
    assert certs["secplus"]["objectives_mastered"] == 1
    assert certs["secplus"]["readiness_percent"] == round(100 / 28)
    assert certs["cysa"]["objectives_mastered"] == 1
    assert certs["sc900"]["objectives_mastered"] == 0
    assert certs["secplus"]["objectives_with_content"] >= 10


def test_daily_activity_series(client, auth_headers, db):
    _complete(client, auth_headers, _lesson(db))
    days = client.get("/api/progress/activity?days=7", headers=auth_headers).json()
    assert len(days) == 7
    assert days[-1]["date"] == utcnow().date().isoformat()
    assert days[-1]["total_minutes"] > 0 and days[-1]["lab_minutes"] > 0


def test_timezone_offset_is_validated(client, auth_headers):
    assert client.get("/api/progress?tz_offset_minutes=99999", headers=auth_headers).status_code == 422


def test_progress_requires_login(client):
    assert client.get("/api/progress").status_code == 401


# --- Streak and time helpers ---------------------------------------------------


def test_streaks():
    today = date(2026, 10, 19)
    days = {today, today - timedelta(days=1), today - timedelta(days=2), today - timedelta(days=5)}
    assert streaks(days, today) == (3, 3)


def test_streak_survives_until_end_of_today():
    today = date(2026, 10, 19)
    assert streaks({today - timedelta(days=1), today - timedelta(days=2)}, today) == (2, 2)


def test_streak_broken_after_missed_day():
    today = date(2026, 10, 19)
    assert streaks({today - timedelta(days=2)}, today) == (0, 1)


def test_local_date_applies_offset():
    late_evening_utc = datetime(2026, 10, 20, 2, 30)
    assert local_date(late_evening_utc, -300) == date(2026, 10, 19)
    assert local_date(late_evening_utc, 0) == date(2026, 10, 20)


def test_week_counts_from_monday(db, client, auth_headers):
    user = db.query(User).one()
    now = datetime(2026, 10, 21, 12, 0)  # a Wednesday
    db.add_all(
        [
            ActivityLog(user_id=user.id, kind="lab", minutes=60, created_at=datetime(2026, 10, 19, 9, 0)),  # Monday
            ActivityLog(user_id=user.id, kind="lab", minutes=45, created_at=datetime(2026, 10, 18, 9, 0)),  # Sunday
        ]
    )
    db.commit()
    week = ProgressStats(db, user, now=now).week()
    assert week["week_start"] == "2026-10-19"
    assert week["minutes"] == 60
    assert week["goal_percent"] == 10


def test_daily_minutes_buckets_by_kind(db, client, auth_headers):
    user = db.query(User).one()
    now = utcnow()
    db.add_all(
        [
            ActivityLog(user_id=user.id, kind="theory", minutes=10, created_at=now),
            ActivityLog(user_id=user.id, kind="lab", minutes=40, created_at=now),
        ]
    )
    db.commit()
    today = ProgressStats(db, user, now=now).daily_minutes(3)[-1]
    assert (today["theory_minutes"], today["lab_minutes"], today["total_minutes"]) == (10, 40, 50)
