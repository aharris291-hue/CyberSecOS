"""Learner progress: lesson state transitions and study activity."""

from __future__ import annotations

from sqlalchemy import select
from sqlalchemy.orm import Session

from ..database import utcnow
from ..models import ActivityKind, ActivityLog, Lesson, LessonProgress, LessonStatus, User
from .quiz import QuizResult


def progress_by_lesson(db: Session, user: User) -> dict[int, LessonProgress]:
    rows = db.scalars(select(LessonProgress).where(LessonProgress.user_id == user.id))
    return {row.lesson_id: row for row in rows}


def get_or_create_progress(db: Session, user: User, lesson: Lesson) -> LessonProgress:
    progress = db.scalar(
        select(LessonProgress).where(LessonProgress.user_id == user.id, LessonProgress.lesson_id == lesson.id)
    )
    if progress is None:
        progress = LessonProgress(user_id=user.id, lesson_id=lesson.id, status=LessonStatus.IN_PROGRESS)
        db.add(progress)
    return progress


def _refresh_status(progress: LessonProgress) -> None:
    """A lesson is complete once its lab is done and its quiz is passed."""
    if progress.lab_completed_at is not None and progress.quiz_passed_at is not None:
        if progress.status != LessonStatus.COMPLETED:
            progress.status = LessonStatus.COMPLETED
            progress.completed_at = utcnow()
    else:
        progress.status = LessonStatus.IN_PROGRESS


def log_activity(db: Session, user: User, kind: ActivityKind, minutes: int, lesson: Lesson | None = None) -> None:
    db.add(ActivityLog(user_id=user.id, lesson_id=lesson.id if lesson else None, kind=kind, minutes=minutes))


def record_quiz_result(db: Session, user: User, lesson: Lesson, result: QuizResult) -> LessonProgress:
    progress = get_or_create_progress(db, user, lesson)
    if progress.best_quiz_score is None or result.score > progress.best_quiz_score:
        progress.best_quiz_score = result.score
    if result.passed and progress.quiz_passed_at is None:
        progress.quiz_passed_at = utcnow()
    _refresh_status(progress)
    # Rough time-on-task: about one minute per question.
    log_activity(db, user, ActivityKind.QUIZ, max(1, result.total), lesson)
    return progress
