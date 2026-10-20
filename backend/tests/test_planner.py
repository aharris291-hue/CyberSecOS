"""Study planner: pace analysis and the weekly schedule."""

from datetime import date, timedelta

from app.models import Lesson


def _lesson(db, slug="net-osi-tcpip"):
    return db.query(Lesson).filter_by(slug=slug).one()


def _pass_quiz(client, headers, lesson):
    answers = [q.correct_index for q in lesson.questions]
    return client.post(f"/api/lessons/{lesson.id}/quiz", headers=headers, json={"answers": answers}).json()


def _complete(client, headers, lesson):
    client.post(f"/api/lessons/{lesson.id}/theory", headers=headers)
    client.post(f"/api/lessons/{lesson.id}/lab", headers=headers, json={"notes": "done"})
    _pass_quiz(client, headers, lesson)


def _set_goal(client, headers, hours):
    assert client.patch("/api/users/me", headers=headers, json={"weekly_goal_hours": hours}).status_code == 200


def _set_target(client, headers, cert, exam_date):
    assert (
        client.put(
            f"/api/certifications/{cert}/exam/target", headers=headers, json={"exam_date": exam_date.isoformat()}
        ).status_code
        == 200
    )


def _flatten(plan):
    return [lesson for week in plan["schedule"] for lesson in week["lessons"]]


def test_plan_requires_login(client):
    assert client.get("/api/plan").status_code == 401


def test_fresh_plan_schedules_every_lesson(client, auth_headers):
    plan = client.get("/api/plan", headers=auth_headers).json()
    totals = client.get("/api/progress", headers=auth_headers).json()["totals"]

    assert plan["total_remaining_lessons"] == totals["total_lessons"]
    assert plan["weekly_goal_hours"] == 10
    assert plan["weekly_target_minutes"] == 600
    # With no exam dates set there is no pace verdict and no focus.
    assert plan["on_track"] is None
    assert plan["focus_certification_id"] is None
    # Every remaining lesson appears exactly once in the schedule.
    scheduled = _flatten(plan)
    assert len(scheduled) == plan["total_remaining_lessons"]
    assert len({lesson["id"] for lesson in scheduled}) == len(scheduled)


def test_schedule_respects_the_weekly_target(client, auth_headers):
    plan = client.get("/api/plan", headers=auth_headers).json()
    target = plan["weekly_target_minutes"]
    for week in plan["schedule"]:
        # A week may exceed the target only when it holds a single lesson
        # longer than the whole weekly budget.
        assert week["planned_minutes"] <= target or len(week["lessons"]) == 1
    # Weeks are numbered from 1 and dated a week apart.
    assert [w["week_index"] for w in plan["schedule"]] == list(range(1, len(plan["schedule"]) + 1))


def test_completing_a_lesson_removes_it_from_the_plan(client, auth_headers, db):
    before = client.get("/api/plan", headers=auth_headers).json()
    lesson = _lesson(db)
    _complete(client, auth_headers, lesson)
    after = client.get("/api/plan", headers=auth_headers).json()

    assert after["total_remaining_lessons"] == before["total_remaining_lessons"] - 1
    assert lesson.id not in {planned["id"] for planned in _flatten(after)}


def test_undated_certification_reports_no_date(client, auth_headers):
    plan = client.get("/api/plan", headers=auth_headers).json()
    for cert in plan["certifications"]:
        assert cert["target_date"] is None
        assert cert["status"] == "no_date"
        assert cert["remaining_lessons"] > 0


def test_distant_exam_is_on_track(client, auth_headers):
    _set_target(client, auth_headers, "secplus", date.today() + timedelta(days=365))
    plan = client.get("/api/plan", headers=auth_headers).json()
    secplus = next(c for c in plan["certifications"] if c["id"] == "secplus")

    assert secplus["status"] == "on_track"
    assert secplus["days_until_exam"] == 365
    assert secplus["required_minutes_per_week"] <= plan["weekly_target_minutes"]
    assert plan["focus_certification_id"] == "secplus"
    assert plan["on_track"] is True


def test_near_exam_with_low_goal_is_behind(client, auth_headers):
    _set_goal(client, auth_headers, 1)  # 60 minutes a week
    _set_target(client, auth_headers, "secplus", date.today() + timedelta(days=7))
    plan = client.get("/api/plan", headers=auth_headers).json()
    secplus = next(c for c in plan["certifications"] if c["id"] == "secplus")

    assert secplus["status"] == "behind"
    assert secplus["required_minutes_per_week"] > plan["weekly_target_minutes"]
    assert plan["on_track"] is False


def test_past_exam_with_work_left_is_overdue(client, auth_headers):
    _set_target(client, auth_headers, "secplus", date.today() - timedelta(days=2))
    plan = client.get("/api/plan", headers=auth_headers).json()
    secplus = next(c for c in plan["certifications"] if c["id"] == "secplus")

    assert secplus["status"] == "overdue"
    assert secplus["days_until_exam"] == -2


def test_schedule_puts_dated_exam_work_first(client, auth_headers):
    # Give one certification a near exam date; its lessons should lead the plan.
    _set_target(client, auth_headers, "secplus", date.today() + timedelta(days=14))
    plan = client.get("/api/plan", headers=auth_headers).json()
    scheduled = _flatten(plan)

    first_undated = next((i for i, lesson in enumerate(scheduled) if lesson["target_date"] is None), len(scheduled))
    last_dated = max((i for i, lesson in enumerate(scheduled) if lesson["target_date"] is not None), default=-1)
    # Every dated lesson comes before the first undated one.
    assert last_dated < first_undated
    assert any(lesson["target_date"] is not None for lesson in scheduled)


def test_low_weekly_goal_stays_bounded(client, auth_headers):
    _set_goal(client, auth_headers, 1)  # 60 minutes a week; lessons run longer
    plan = client.get("/api/plan", headers=auth_headers).json()

    assert plan["weekly_target_minutes"] == 60
    assert len(plan["schedule"]) <= 52
    # One lesson per week when each lesson already exceeds the weekly budget.
    assert all(len(week["lessons"]) == 1 for week in plan["schedule"])
    assert plan["estimated_weeks_to_finish"] >= plan["total_remaining_lessons"]
    assert plan["estimated_finish_date"] is not None
