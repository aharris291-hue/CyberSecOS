"""Advanced analytics: study trends, patterns, velocity and quiz weak-spots."""

from __future__ import annotations

from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from ..database import get_db
from ..models import User
from ..schemas import Analytics
from ..security import get_current_user
from ..services.analytics import LearnerAnalytics
from ..services.stats import ProgressStats
from .progress import TzOffset

router = APIRouter(prefix="/api", tags=["analytics"])


@router.get("/analytics", response_model=Analytics)
def analytics(
    weeks: int = Query(8, ge=1, le=52),
    tz_offset_minutes: int = TzOffset,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> dict:
    stats = ProgressStats(db, user, tz_offset_minutes)
    return LearnerAnalytics(stats, weeks=weeks).analytics()
