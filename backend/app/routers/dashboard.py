"""Dashboard: everything the home screen needs in one request."""

from __future__ import annotations

from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from ..database import get_db
from ..models import User
from ..schemas import Dashboard
from ..security import get_current_user
from ..services.stats import ProgressStats
from .progress import TzOffset

router = APIRouter(prefix="/api", tags=["dashboard"])

DASHBOARD_ACTIVITY_DAYS = 14


@router.get("/dashboard", response_model=Dashboard)
def dashboard(
    tz_offset_minutes: int = TzOffset,
    days: int = Query(default=DASHBOARD_ACTIVITY_DAYS, ge=7, le=90),
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> dict:
    stats = ProgressStats(db, user, tz_offset_minutes)
    return {
        "display_name": user.display_name,
        "target_role": user.target_role,
        "totals": stats.totals(),
        "week": stats.week(),
        "next_lesson": stats.next_lesson(),
        "daily_activity": stats.daily_minutes(days),
        "courses": stats.by_course(),
        "certifications": stats.by_certification(),
        "recent_activity": stats.recent_activity(limit=5),
    }
