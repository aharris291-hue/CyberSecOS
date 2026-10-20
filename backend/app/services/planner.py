"""Study planner: pace analysis and a week-by-week schedule toward each exam.

Read-only, like :class:`~app.services.stats.ProgressStats`. The planner turns the
learner's weekly goal, their target exam dates and their current progress into
two things: a per-certification pace check (are they on track to be ready in
time?) and a schedule that packs the remaining lessons into weeks at the
learner's chosen pace, soonest-exam work first.
"""

from __future__ import annotations

import math
from datetime import date, timedelta

from sqlalchemy import select
from sqlalchemy.orm import selectinload

from ..models import Certification, Lesson
from .stats import ProgressStats

# Cap the generated schedule so a very low weekly goal cannot produce an
# unbounded response; the summary still reports the full remaining workload.
MAX_PLAN_WEEKS = 52


class StudyPlanner:
    """Builds the study plan for one learner from an existing :class:`ProgressStats`."""

    def __init__(self, stats: ProgressStats):
        self.stats = stats
        self.today = stats.today
        self.weekly_target_minutes = max(0, stats.user.weekly_goal_hours * 60)
        self.completed = stats.completed_ids()
        self._certs = self._cert_data()

    # --- Helpers ----------------------------------------------------------

    @staticmethod
    def _lesson_minutes(lesson: Lesson) -> int:
        return lesson.theory_minutes + lesson.lab_minutes

    def _incomplete_lessons(self) -> list[Lesson]:
        """Not-yet-completed lessons in course then lesson order."""
        return [lesson for course in self.stats.courses for lesson in course.lessons if lesson.id not in self.completed]

    def _cert_data(self) -> list[dict]:
        """Per-certification working set: uncovered objectives and the lessons that cover them."""
        incomplete = self._incomplete_lessons()
        data = []
        for cert in self.stats.db.scalars(
            select(Certification).options(selectinload(Certification.objectives)).order_by(Certification.priority)
        ):
            obj_ids = {o.id for o in cert.objectives}
            mastered: set[str] = set()
            for lesson in self.stats.lessons:
                if lesson.id in self.completed:
                    mastered |= obj_ids & {o.id for o in lesson.objectives}
            uncovered = obj_ids - mastered
            cert_lessons = [lesson for lesson in incomplete if uncovered & {o.id for o in lesson.objectives}]
            data.append(
                {
                    "cert": cert,
                    "target": self.stats.exam_targets.get(cert.id),
                    "uncovered": uncovered,
                    "lessons": cert_lessons,
                }
            )
        return data

    # --- Pace -------------------------------------------------------------

    def pace(self) -> list[dict]:
        """For each certification, the work left and whether the weekly goal clears it in time."""
        result = []
        for entry in self._certs:
            cert = entry["cert"]
            cert_lessons = entry["lessons"]
            target_date = entry["target"]
            remaining_minutes = sum(self._lesson_minutes(lesson) for lesson in cert_lessons)
            days = (target_date - self.today).days if target_date else None
            weeks = max(1, math.ceil(days / 7)) if days is not None and days > 0 else None
            required = math.ceil(remaining_minutes / weeks) if weeks else None
            if not cert_lessons:
                status = "ready"
            elif target_date is None:
                status = "no_date"
            elif days is not None and days < 0:
                status = "overdue"
            elif required is not None and required <= self.weekly_target_minutes:
                status = "on_track"
            else:
                status = "behind"
            result.append(
                {
                    "id": cert.id,
                    "name": cert.name,
                    "exam_code": cert.exam_code,
                    "target_date": target_date.isoformat() if target_date else None,
                    "days_until_exam": days,
                    "remaining_lessons": len(cert_lessons),
                    "remaining_minutes": remaining_minutes,
                    "required_minutes_per_week": required,
                    "status": status,
                }
            )
        return result

    # --- Schedule ---------------------------------------------------------

    def _earliest_target_by_lesson(self) -> dict[int, date]:
        """Soonest exam date each incomplete lesson contributes to, if any."""
        earliest: dict[int, date] = {}
        for entry in self._certs:
            target = entry["target"]
            if target is None:
                continue
            for lesson in entry["lessons"]:
                current = earliest.get(lesson.id)
                if current is None or target < current:
                    earliest[lesson.id] = target
        return earliest

    def schedule(self) -> list[dict]:
        """Pack remaining lessons into weeks at the weekly goal, soonest-exam work first."""
        if self.weekly_target_minutes <= 0:
            return []

        earliest = self._earliest_target_by_lesson()
        incomplete = self._incomplete_lessons()
        order_index = {lesson.id: i for i, lesson in enumerate(incomplete)}
        # Lessons tied to a dated exam come first (soonest date wins); the rest
        # follow in course order. date.max keeps undated lessons last.
        ordered = sorted(incomplete, key=lambda lesson: (earliest.get(lesson.id, date.max), order_index[lesson.id]))

        monday = self.today - timedelta(days=self.today.weekday())
        weeks: list[dict] = []
        current: list[dict] = []
        planned = 0

        def flush() -> None:
            weeks.append(
                {
                    "week_index": len(weeks) + 1,
                    "week_start": (monday + timedelta(weeks=len(weeks))).isoformat(),
                    "planned_minutes": planned,
                    "lessons": current,
                }
            )

        for lesson in ordered:
            minutes = self._lesson_minutes(lesson)
            # Start a new week when this lesson would overflow a non-empty one.
            # A lesson longer than the whole weekly goal still gets its own week.
            if current and planned + minutes > self.weekly_target_minutes:
                flush()
                if len(weeks) >= MAX_PLAN_WEEKS:
                    return weeks
                current = []
                planned = 0
            current.append(
                {
                    "id": lesson.id,
                    "title": lesson.title,
                    "course_title": lesson.course.title,
                    "theory_minutes": lesson.theory_minutes,
                    "lab_minutes": lesson.lab_minutes,
                    "target_date": earliest[lesson.id].isoformat() if lesson.id in earliest else None,
                }
            )
            planned += minutes

        if current and len(weeks) < MAX_PLAN_WEEKS:
            flush()
        return weeks

    # --- Plan -------------------------------------------------------------

    def plan(self) -> dict:
        """The full study plan: summary, per-certification pace and the weekly schedule."""
        pace = self.pace()
        schedule = self.schedule()
        incomplete = self._incomplete_lessons()
        total_minutes = sum(self._lesson_minutes(lesson) for lesson in incomplete)
        weekly = self.weekly_target_minutes
        estimated_weeks = math.ceil(total_minutes / weekly) if weekly and total_minutes else 0

        dated = [p for p in pace if p["target_date"] is not None]
        on_track = all(p["status"] in ("on_track", "ready") for p in dated) if dated else None

        # Headline focus: the soonest exam that still has lessons left.
        candidates = [p for p in pace if p["target_date"] is not None and p["remaining_lessons"] > 0]
        focus = min(candidates, key=lambda p: p["target_date"])["id"] if candidates else None

        return {
            "weekly_goal_hours": self.stats.user.weekly_goal_hours,
            "weekly_target_minutes": weekly,
            "total_remaining_lessons": len(incomplete),
            "total_remaining_minutes": total_minutes,
            "estimated_weeks_to_finish": estimated_weeks,
            "estimated_finish_date": (self.today + timedelta(weeks=estimated_weeks)).isoformat()
            if estimated_weeks
            else None,
            "on_track": on_track,
            "focus_certification_id": focus,
            "certifications": pace,
            "schedule": schedule,
        }
