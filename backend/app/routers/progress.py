"""Progress tracking: theory and lab completion, and progress summaries."""

from __future__ import annotations

from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from ..database import get_db
from ..models import User
from ..schemas import DailyMinutes, LabRequest, LessonProgressOut, ProgressSummary, TheoryRequest
from ..security import get_current_user
from ..services import progress as progress_service
from ..services.stats import ProgressStats
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


@router.get("/progress/activity", response_model=list[DailyMinutes])
def daily_activity(
    days: int = Query(default=30, ge=1, le=365),
    tz_offset_minutes: int = TzOffset,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> list[dict]:
    return ProgressStats(db, user, tz_offset_minutes).daily_minutes(days)
