"""Advanced analytics: study trends, patterns, learning velocity and quiz weak-spots."""

from datetime import datetime, timedelta

from app.database import utcnow
from app.models import ActivityKind, ActivityLog, Lesson, QuizAttempt, User


def _user(db) -> User:
    return db.query(User).one()


def _lesson(db, slug="net-osi-tcpip") -> Lesson:
    return db.query(Lesson).filter_by(slug=slug).one()


def _log(db, user, *, minutes, when, kind=ActivityKind.LAB, lesson_id=None):
    db.add(
        ActivityLog(
            user_id=user.id, lesson_id=lesson_id, kind=kind, minutes=minutes, created_at=when
        )
    )


def _attempt(db, user, lesson, *, score, when):
    total = len(lesson.questions)
    correct = round(score / 100 * total)
    db.add(
        QuizAttempt(
            user_id=user.id,
            lesson_id=lesson.id,
            score=score,
            correct=correct,
            total=total,
            answers=[0] * total,
            created_at=when,
        )
    )


def _complete(client, headers, lesson):
    client.post(f"/api/lessons/{lesson.id}/theory", headers=headers)
    client.post(f"/api/lessons/{lesson.id}/lab", headers=headers, json={"notes": "done"})
    answers = [q.correct_index for q in lesson.questions]
    client.post(f"/api/lessons/{lesson.id}/quiz", headers=headers, json={"answers": answers})


def test_analytics_requires_login(client):
    assert client.get("/api/analytics").status_code == 401


def test_empty_account_is_all_zero(client, auth_headers):
    data = client.get("/api/analytics", headers=auth_headers).json()

    # Trend defaults to eight weekly buckets, every one empty.
    assert len(data["study_trend"]) == 8
    assert all(w["minutes"] == 0 and w["active_days"] == 0 for w in data["study_trend"])
    assert all(w["rolling_average_minutes"] == 0 for w in data["study_trend"])

    patterns = data["study_patterns"]
    assert len(patterns["by_weekday"]) == 7
    assert [w["label"] for w in patterns["by_weekday"]] == ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]
    assert [p["part"] for p in patterns["by_part_of_day"]] == ["Morning", "Afternoon", "Evening", "Night"]
    assert patterns["peak_part_of_day"] is None

    velocity = data["velocity"]
    assert velocity["completed_lessons"] == 0
    assert velocity["remaining_lessons"] == velocity["total_lessons"] > 0
    assert velocity["lessons_per_week"] == 0.0
    assert velocity["projected_weeks_to_finish"] is None
    assert velocity["projected_finish_date"] is None

    quiz = data["quiz_performance"]
    assert quiz["total_attempts"] == 0
    assert quiz["average_score"] is None
    assert quiz["score_trend"] is None
    assert quiz["weakest_domains"] == []
    assert quiz["weakest_objectives"] == []


def test_weeks_query_bounds(client, auth_headers):
    assert len(client.get("/api/analytics?weeks=4", headers=auth_headers).json()["study_trend"]) == 4
    assert len(client.get("/api/analytics?weeks=1", headers=auth_headers).json()["study_trend"]) == 1
    assert client.get("/api/analytics?weeks=0", headers=auth_headers).status_code == 422
    assert client.get("/api/analytics?weeks=53", headers=auth_headers).status_code == 422


def test_study_trend_buckets_by_week(client, auth_headers, db):
    user = _user(db)
    now = utcnow()
    _log(db, user, minutes=30, when=now)  # this week
    _log(db, user, minutes=45, when=now - timedelta(days=14))  # two weeks back
    db.commit()

    trend = client.get("/api/analytics", headers=auth_headers).json()["study_trend"]
    assert sum(w["minutes"] for w in trend) == 75
    # The newest bucket is the current week and carries today's session.
    assert trend[-1]["minutes"] == 30
    assert trend[-1]["active_days"] == 1
    # The 45-minute session two weeks ago lands in its own earlier bucket.
    assert any(w["minutes"] == 45 for w in trend[:-1])
    # Rolling average over the trailing four weeks of the current bucket: (30+0+45+0)/4.
    assert trend[-1]["rolling_average_minutes"] == round((30 + 45) / 4)


def test_study_patterns_map_weekday_and_part_of_day(client, auth_headers, db):
    user = _user(db)
    # 2026-01-05 is a Monday; 09:00 is Morning, 20:00 is Evening.
    _log(db, user, minutes=20, when=datetime(2026, 1, 5, 9, 0))
    _log(db, user, minutes=40, when=datetime(2026, 1, 5, 20, 0))
    db.commit()

    patterns = client.get("/api/analytics", headers=auth_headers).json()["study_patterns"]
    monday = next(w for w in patterns["by_weekday"] if w["label"] == "Mon")
    assert monday["minutes"] == 60
    parts = {p["part"]: p["minutes"] for p in patterns["by_part_of_day"]}
    assert parts["Morning"] == 20
    assert parts["Evening"] == 40
    assert patterns["peak_part_of_day"] == "Evening"


def test_velocity_projects_from_recent_pace(client, auth_headers, db):
    # Completing lessons through the API records them as completed now, so they
    # count as recent progress and drive the per-week pace and projection.
    for slug in ("net-osi-tcpip", "net-ports-protocols"):
        _complete(client, auth_headers, _lesson(db, slug))

    velocity = client.get("/api/analytics", headers=auth_headers).json()["velocity"]
    assert velocity["completed_lessons"] == 2
    assert velocity["completed_last_4_weeks"] == 2
    assert velocity["lessons_per_week"] == 0.5  # 2 lessons over a four-week window
    assert velocity["remaining_lessons"] == velocity["total_lessons"] - 2
    # At half a lesson a week, the projection covers every remaining lesson.
    assert velocity["projected_weeks_to_finish"] == velocity["remaining_lessons"] * 2
    assert velocity["projected_finish_date"] is not None


def test_quiz_performance_trend_and_weak_spots(client, auth_headers, db):
    user = _user(db)
    lesson = _lesson(db)
    now = utcnow()
    # Four attempts, improving over time: earlier half 40/50, later half 80/90.
    for i, score in enumerate((40, 50, 80, 90)):
        _attempt(db, user, lesson, score=score, when=now - timedelta(days=4 - i))
    db.commit()

    quiz = client.get("/api/analytics", headers=auth_headers).json()["quiz_performance"]
    assert quiz["total_attempts"] == 4
    assert quiz["average_score"] == round((40 + 50 + 80 + 90) / 4)
    # Later half mean (85) minus earlier half mean (45) is a positive trend.
    assert quiz["score_trend"] == 40


def test_weak_spots_rank_lowest_scoring_areas(client, auth_headers, db):
    # Pass one lesson outright (high best score) and score another poorly, so the
    # weaker lesson's domain and objectives rank ahead of the stronger one.
    strong = _lesson(db, "net-osi-tcpip")
    weak = _lesson(db, "linux-auth-logs")
    _complete(client, auth_headers, strong)  # best score 100
    # A low-scoring quiz submission on the weak lesson.
    client.post(
        f"/api/lessons/{weak.id}/quiz",
        headers=auth_headers,
        json={"answers": [(q.correct_index + 1) % len(q.options) for q in weak.questions]},
    )

    quiz = client.get("/api/analytics", headers=auth_headers).json()["quiz_performance"]
    domains = [d["domain"] for d in quiz["weakest_domains"]]
    assert weak.course.domain in domains
    # The weakest domain is listed first and scores below the strong lesson's domain.
    assert quiz["weakest_domains"][0]["average_score"] <= quiz["weakest_domains"][-1]["average_score"]
    assert all(0 <= d["average_score"] <= 100 for d in quiz["weakest_domains"])
    assert all(o["certification"] for o in quiz["weakest_objectives"])
