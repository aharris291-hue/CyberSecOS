"""The downloadable PDF progress report."""

from __future__ import annotations

from datetime import date

from app.models import Lesson
from app.services.report import _format_hours, build_progress_pdf


def _lesson(db, slug="net-osi-tcpip"):
    return db.query(Lesson).filter_by(slug=slug).one()


def _complete(client, headers, lesson):
    client.post(f"/api/lessons/{lesson.id}/theory", headers=headers)
    client.post(f"/api/lessons/{lesson.id}/lab", headers=headers, json={"notes": "done"})
    answers = [q.correct_index for q in lesson.questions]
    client.post(f"/api/lessons/{lesson.id}/quiz", headers=headers, json={"answers": answers})


# --- Endpoint ---------------------------------------------------------------


def test_report_requires_authentication(client):
    assert client.get("/api/progress/report.pdf").status_code == 401


def test_report_returns_a_pdf(client, auth_headers, db):
    _complete(client, auth_headers, _lesson(db))
    response = client.get("/api/progress/report.pdf", headers=auth_headers)
    assert response.status_code == 200
    assert response.headers["content-type"] == "application/pdf"
    body = response.content
    assert body.startswith(b"%PDF-")
    assert b"%%EOF" in body
    assert len(body) > 1000


def test_report_filename_is_a_dated_attachment(client, auth_headers):
    response = client.get("/api/progress/report.pdf", headers=auth_headers)
    disposition = response.headers["content-disposition"]
    assert disposition.startswith("attachment;")
    assert "cybersecos-progress-" in disposition and disposition.rstrip('"').endswith(".pdf")


def test_report_works_for_a_fresh_account_with_no_activity(client, auth_headers):
    """A learner who has done nothing yet still gets a valid report, not an error."""
    response = client.get("/api/progress/report.pdf", headers=auth_headers)
    assert response.status_code == 200
    assert response.content.startswith(b"%PDF-")


# --- Builder ----------------------------------------------------------------


def test_format_hours():
    assert _format_hours(0) == "0m"
    assert _format_hours(35) == "35m"
    assert _format_hours(60) == "1h"
    assert _format_hours(95) == "1h 35m"


def test_builder_handles_missing_quiz_score_and_role():
    """None average score, no target role and an unset exam date must not raise."""
    totals = {
        "total_lessons": 72,
        "completed_lessons": 0,
        "completion_percent": 0,
        "labs_completed": 0,
        "quizzes_passed": 0,
        "average_quiz_score": None,
        "total_minutes": 0,
        "hands_on_percent": None,
        "current_streak_days": 0,
        "longest_streak_days": 0,
    }
    week = {"minutes": 0, "goal_minutes": 300, "goal_percent": 0}
    courses = [{"title": "Networking", "completed_lessons": 0, "total_lessons": 6, "percent": 0}]
    certs = [
        {
            "name": "Security+",
            "exam_code": "SY0-701",
            "objectives_total": 30,
            "objectives_mastered": 0,
            "readiness_percent": 0,
            "target_date": None,
            "days_until_exam": None,
        }
    ]
    pdf = build_progress_pdf(
        display_name="Adam",
        target_role=None,
        totals=totals,
        week=week,
        courses=courses,
        certifications=certs,
        generated_on=date(2026, 10, 20),
    )
    assert pdf.startswith(b"%PDF-") and b"%%EOF" in pdf


def test_builder_renders_overdue_exam_date():
    totals = {
        "total_lessons": 10,
        "completed_lessons": 5,
        "completion_percent": 50,
        "labs_completed": 5,
        "quizzes_passed": 5,
        "average_quiz_score": 88,
        "total_minutes": 600,
        "hands_on_percent": 80,
        "current_streak_days": 3,
        "longest_streak_days": 7,
    }
    week = {"minutes": 120, "goal_minutes": 300, "goal_percent": 40}
    courses = [{"title": "SIEM", "completed_lessons": 5, "total_lessons": 6, "percent": 83}]
    certs = [
        {
            "name": "SC-200",
            "exam_code": "SC-200",
            "objectives_total": 4,
            "objectives_mastered": 2,
            "readiness_percent": 50,
            "target_date": "2026-01-01",
            "days_until_exam": -30,
        }
    ]
    pdf = build_progress_pdf(
        display_name="Adam",
        target_role="SOC Analyst",
        totals=totals,
        week=week,
        courses=courses,
        certifications=certs,
        generated_on=date(2026, 10, 20),
    )
    assert pdf.startswith(b"%PDF-")
