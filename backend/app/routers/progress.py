"""Progress tracking: theory and lab completion, and progress summaries."""

from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, Query, Response, status
from sqlalchemy.orm import Session

from ..database import get_db, utcnow
from ..models import User
from ..schemas import DailyMinutes, LabRequest, LessonProgressOut, ProgressSummary, TheoryRequest
from ..security import get_current_user
from ..services import progress as progress_service
from ..services.report import build_progress_pdf
from ..services.stats import ProgressStats, local_date
from .courses import get_lesson_or_404, progress_out

router = APIRouter(prefix="/api", tags=["progress"])

# Minutes east of UTC, e.g. -300 for US Eastern Standard Time. Used to bucket days.
TzOffset = Query(default=0, ge=-14 * 60, le=14 * 60, description="Learner's UTC offset in minutes")


@router.post("/lessons/{lesson_id}/theory", response_model=LessonProgressOut)
def mark_theory_read(
    lesson_id: int,
    body: TheoryRequest | None = None,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> LessonProgressOut:
    lesson = get_lesson_or_404(db, lesson_id)
    progress = progress_service.mark_theory_read(db, user, lesson, body.minutes if body else None)
    db.commit()
    return progress_out(progress)


@router.post("/lessons/{lesson_id}/lab", response_model=LessonProgressOut)
def complete_lab(
    lesson_id: int, body: LabRequest, db: Session = Depends(get_db), user: User = Depends(get_current_user)
) -> LessonProgressOut:
    lesson = get_lesson_or_404(db, lesson_id)
    if lesson.lab is not None:
        # A graded lab is completed by passing it, not by self-marking. Notes can still be saved.
        current = progress_service.progress_by_lesson(db, user).get(lesson.id)
        if current is None or current.lab_completed_at is None:
            raise HTTPException(
                status.HTTP_409_CONFLICT, "This lesson has a graded lab: pass it to complete the lab"
            )
    progress = progress_service.complete_lab(db, user, lesson, body.notes, body.minutes)
    db.commit()
    return progress_out(progress)


@router.delete("/lessons/{lesson_id}/lab", response_model=LessonProgressOut)
def reopen_lab(lesson_id: int, db: Session = Depends(get_db), user: User = Depends(get_current_user)) -> LessonProgressOut:
    lesson = get_lesson_or_404(db, lesson_id)
    progress = progress_service.reopen_lab(db, user, lesson)
    db.commit()
    return progress_out(progress)


@router.get("/progress", response_model=ProgressSummary)
def progress_summary(
    tz_offset_minutes: int = TzOffset, db: Session = Depends(get_db), user: User = Depends(get_current_user)
) -> dict:
    stats = ProgressStats(db, user, tz_offset_minutes)
    return {
        "totals": stats.totals(),
        "week": stats.week(),
        "courses": stats.by_course(),
        "certifications": stats.by_certification(),
        "recent_activity": stats.recent_activity(),
        "next_lesson": stats.next_lesson(),
    }


@router.get(
    "/progress/report.pdf",
    responses={200: {"content": {"application/pdf": {}}, "description": "A one-page progress report"}},
)
def progress_report(
    tz_offset_minutes: int = TzOffset, db: Session = Depends(get_db), user: User = Depends(get_current_user)
) -> Response:
    """Download the learner's progress as a PDF, drawn from the same figures as /api/progress."""
    now = utcnow()
    stats = ProgressStats(db, user, tz_offset_minutes, now=now)
    pdf = build_progress_pdf(
        display_name=user.display_name,
        target_role=user.target_role,
        totals=stats.totals(),
        week=stats.week(),
        courses=stats.by_course(),
        certifications=stats.by_certification(),
        generated_on=local_date(now, tz_offset_minutes),
    )
    filename = f"cybersecos-progress-{local_date(now, tz_offset_minutes).isoformat()}.pdf"
    return Response(
        content=pdf,
        media_type="application/pdf",
        headers={"Content-Disposition": f'attachment; filename="{filename}"'},
    )


@router.get("/progress/activity", response_model=list[DailyMinutes])
def daily_activity(
    days: int = Query(default=30, ge=1, le=365),
    tz_offset_minutes: int = TzOffset,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> list[dict]:
    return ProgressStats(db, user, tz_offset_minutes).daily_minutes(days)
