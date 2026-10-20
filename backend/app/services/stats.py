"""Read-only progress statistics used by the progress and dashboard endpoints."""

from __future__ import annotations

from collections import defaultdict
from datetime import date, datetime, timedelta

from sqlalchemy import select
from sqlalchemy.orm import Session, selectinload

from ..database import utcnow
from ..models import ActivityLog, Certification, Course, ExamTarget, Lesson, LessonProgress, LessonStatus, QuizAttempt, User


def local_date(timestamp: datetime, tz_offset_minutes: int) -> date:
    """Calendar day of a stored UTC timestamp in the learner's time zone."""
    return (timestamp + timedelta(minutes=tz_offset_minutes)).date()


def streaks(active_days: set[date], today: date) -> tuple[int, int]:
    """Return (current, longest) runs of consecutive study days.

    The current streak survives until the end of today, so studying yesterday
    but not yet today still counts.
    """
    longest = run = 0
    previous: date | None = None
    for day in sorted(active_days):
        run = run + 1 if previous is not None and day - previous == timedelta(days=1) else 1
        longest = max(longest, run)
        previous = day

    current = 0
    cursor = today if today in active_days else today - timedelta(days=1)
    while cursor in active_days:
        current += 1
        cursor -= timedelta(days=1)
    return current, longest


class ProgressStats:
    """Computes every progress figure for one learner from a single set of queries."""

    def __init__(self, db: Session, user: User, tz_offset_minutes: int = 0, now: datetime | None = None):
        self.db = db
        self.user = user
        self.tz = tz_offset_minutes
        self.now = now or utcnow()
        self.today = local_date(self.now, self.tz)
        self.courses = list(
            db.scalars(
                select(Course)
                .options(selectinload(Course.lessons).selectinload(Lesson.objectives))
                .order_by(Course.order)
            )
        )
        self.lessons = [lesson for course in self.courses for lesson in course.lessons]
        self.progress = {
            row.lesson_id: row for row in db.scalars(select(LessonProgress).where(LessonProgress.user_id == user.id))
        }
        self.activity = list(
            db.scalars(select(ActivityLog).where(ActivityLog.user_id == user.id).order_by(ActivityLog.created_at))
        )
        self.exam_targets = {
            t.certification_id: t.exam_date
            for t in db.scalars(select(ExamTarget).where(ExamTarget.user_id == user.id))
        }

    def status(self, lesson: Lesson) -> str:
        row = self.progress.get(lesson.id)
        return row.status if row else LessonStatus.NOT_STARTED

    def completed_ids(self) -> set[int]:
        return {lid for lid, row in self.progress.items() if row.status == LessonStatus.COMPLETED}

    # --- Totals -----------------------------------------------------------

    def totals(self) -> dict:
        rows = list(self.progress.values())
        scores = [row.best_quiz_score for row in rows if row.best_quiz_score is not None]
        minutes = defaultdict(int)
        for entry in self.activity:
            minutes[entry.kind] += entry.minutes
        hands_on = minutes["lab"]
        studied = sum(minutes.values())
        current, longest = streaks(self.active_days(), self.today)
        total_lessons = len(self.lessons)
        completed = len(self.completed_ids())
        return {
            "total_lessons": total_lessons,
            "completed_lessons": completed,
            "in_progress_lessons": sum(row.status == LessonStatus.IN_PROGRESS for row in rows),
            "completion_percent": round(100 * completed / total_lessons) if total_lessons else 0,
            "labs_completed": sum(row.lab_completed_at is not None for row in rows),
            "quizzes_passed": sum(row.quiz_passed_at is not None for row in rows),
            "quiz_attempts": self.db.query(QuizAttempt).filter_by(user_id=self.user.id).count(),
            "average_quiz_score": round(sum(scores) / len(scores)) if scores else None,
            "theory_minutes": minutes["theory"],
            "lab_minutes": hands_on,
            "quiz_minutes": minutes["quiz"],
            "total_minutes": studied,
            "hands_on_percent": round(100 * hands_on / studied) if studied else None,
            "current_streak_days": current,
            "longest_streak_days": longest,
        }

    # --- Time -------------------------------------------------------------

    def active_days(self) -> set[date]:
        return {local_date(entry.created_at, self.tz) for entry in self.activity}

    def daily_minutes(self, days: int) -> list[dict]:
        start = self.today - timedelta(days=days - 1)
        buckets = {start + timedelta(days=i): defaultdict(int) for i in range(days)}
        for entry in self.activity:
            day = local_date(entry.created_at, self.tz)
            if day in buckets:
                buckets[day][entry.kind] += entry.minutes
        return [
            {
                "date": day.isoformat(),
                "theory_minutes": kinds["theory"],
                "lab_minutes": kinds["lab"],
                "quiz_minutes": kinds["quiz"],
                "total_minutes": sum(kinds.values()),
            }
            for day, kinds in buckets.items()
        ]

    def week(self) -> dict:
        """Study time since Monday (learner's time zone) against the weekly goal."""
        monday = self.today - timedelta(days=self.today.weekday())
        minutes = sum(e.minutes for e in self.activity if local_date(e.created_at, self.tz) >= monday)
        goal = self.user.weekly_goal_hours * 60
        return {
            "week_start": monday.isoformat(),
            "minutes": minutes,
            "goal_minutes": goal,
            "goal_percent": min(100, round(100 * minutes / goal)) if goal else 0,
        }

    # --- Breakdowns -------------------------------------------------------

    def by_course(self) -> list[dict]:
        result = []
        for course in self.courses:
            completed = sum(self.status(lesson) == LessonStatus.COMPLETED for lesson in course.lessons)
            total = len(course.lessons)
            result.append(
                {
                    "slug": course.slug,
                    "title": course.title,
                    "domain": course.domain,
                    "total_lessons": total,
                    "completed_lessons": completed,
                    "percent": round(100 * completed / total) if total else 0,
                }
            )
        return result

    def by_certification(self) -> list[dict]:
        """Readiness = share of a certification's objectives covered by completed lessons."""
        completed = self.completed_ids()
        mastered: set[str] = set()
        with_content: set[str] = set()
        for lesson in self.lessons:
            for objective in lesson.objectives:
                with_content.add(objective.id)
                if lesson.id in completed:
                    mastered.add(objective.id)
        result = []
        for cert in self.db.scalars(
            select(Certification).options(selectinload(Certification.objectives)).order_by(Certification.priority)
        ):
            ids = {o.id for o in cert.objectives}
            total = len(ids)
            done = len(ids & mastered)
            target_date = self.exam_targets.get(cert.id)
            result.append(
                {
                    "id": cert.id,
                    "name": cert.name,
                    "exam_code": cert.exam_code,
                    "objectives_total": total,
                    "objectives_with_content": len(ids & with_content),
                    "objectives_mastered": done,
                    "readiness_percent": round(100 * done / total) if total else 0,
                    "target_date": target_date.isoformat() if target_date else None,
                    "days_until_exam": (target_date - self.today).days if target_date else None,
                }
            )
        return result

    def recent_activity(self, limit: int = 10) -> list[dict]:
        titles = {lesson.id: lesson.title for lesson in self.lessons}
        return [
            {
                "kind": entry.kind,
                "minutes": entry.minutes,
                "lesson_id": entry.lesson_id,
                "lesson_title": titles.get(entry.lesson_id),
                "created_at": entry.created_at,
            }
            for entry in reversed(self.activity[-limit:])
        ]

    def next_lesson(self) -> dict | None:
        """Continue an unfinished lesson first, otherwise start the next new one in course order."""
        for wanted in (LessonStatus.IN_PROGRESS, LessonStatus.NOT_STARTED):
            for course in self.courses:
                for lesson in course.lessons:
                    if self.status(lesson) == wanted:
                        return {
                            "id": lesson.id,
                            "title": lesson.title,
                            "course_title": course.title,
                            "status": wanted,
                            "lab_minutes": lesson.lab_minutes,
                            "theory_minutes": lesson.theory_minutes,
                        }
        return None
