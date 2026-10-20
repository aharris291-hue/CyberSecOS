"""Study planner: a pace check and a weekly schedule toward the learner's exams."""

from __future__ import annotations

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from ..database import get_db
from ..models import User
from ..schemas import StudyPlan
from ..security import get_current_user
from ..services.planner import StudyPlanner
from ..services.stats import ProgressStats
from .progress import TzOffset

router = APIRouter(prefix="/api", tags=["planner"])


@router.get("/plan", response_model=StudyPlan)
def study_plan(
    tz_offset_minutes: int = TzOffset,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> dict:
    stats = ProgressStats(db, user, tz_offset_minutes)
    return StudyPlanner(stats).plan()
