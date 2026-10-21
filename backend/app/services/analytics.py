"""Advanced learner analytics: study trends, patterns, velocity and quiz weak-spots.

Read-only, like :class:`~app.services.stats.ProgressStats`, and built on the same
activity, progress and quiz records the rest of the app already writes. Where the
dashboard shows the current state, analytics looks across time: how study is
trending, when the learner studies, how fast they are really moving, and which
areas their quiz scores say to revise.
"""

from __future__ import annotations

import math
from collections import defaultdict
from datetime import date, datetime, timedelta

from sqlalchemy import select

from ..models import Certification, LessonStatus, QuizAttempt
from .stats import ProgressStats, local_date

WEEKDAY_LABELS = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"]
# (label, first hour inclusive, last hour inclusive) in the learner's local time.
PARTS_OF_DAY = [
    ("Morning", 5, 11),
    ("Afternoon", 12, 16),
    ("Evening", 17, 21),
    ("Night", 22, 4),  # wraps midnight
]
ROLLING_WINDOW_WEEKS = 4
VELOCITY_WINDOW_DAYS = 28


def _part_of_day(hour: int) -> str:
    for label, start, end in PARTS_OF_DAY:
        if start <= end:
            if start <= hour <= end:
                return label
        elif hour >= start or hour <= end:  # wraps midnight (Night)
            return label
    return "Night"


class LearnerAnalytics:
    """Computes the analytics payload for one learner from an existing ProgressStats."""

    def __init__(self, stats: ProgressStats, weeks: int = 8):
        self.stats = stats
        self.today = stats.today
        self.weeks = weeks
        self._local = {entry.id: self._local_dt(entry.created_at) for entry in stats.activity}

    def _local_dt(self, timestamp: datetime) -> datetime:
        return timestamp + timedelta(minutes=self.stats.tz)

    # --- Study trend ------------------------------------------------------

    def study_trend(self) -> list[dict]:
        """Weekly study minutes and active days for the last `weeks` weeks, with a rolling average."""
        monday = self.today - timedelta(days=self.today.weekday())
        starts = [monday - timedelta(weeks=self.weeks - 1 - i) for i in range(self.weeks)]
        minutes: list[int] = []
        active: list[int] = []
        for start in starts:
            end = start + timedelta(days=7)
            days: set[date] = set()
            total = 0
            for entry in self.stats.activity:
                day = self._local[entry.id].date()
                if start <= day < end:
                    total += entry.minutes
                    days.add(day)
            minutes.append(total)
            active.append(len(days))
        result = []
        for i, start in enumerate(starts):
            window = minutes[max(0, i - ROLLING_WINDOW_WEEKS + 1) : i + 1]
            result.append(
                {
                    "week_start": start.isoformat(),
                    "minutes": minutes[i],
                    "active_days": active[i],
                    "rolling_average_minutes": round(sum(window) / len(window)),
                }
            )
        return result

    # --- Study patterns ---------------------------------------------------

    def study_patterns(self) -> dict:
        """When the learner studies: minutes by weekday and by part of the day."""
        by_weekday = [0] * 7
        by_part: dict[str, int] = defaultdict(int)
        for entry in self.stats.activity:
            when = self._local[entry.id]
            by_weekday[when.weekday()] += entry.minutes
            by_part[_part_of_day(when.hour)] += entry.minutes
        parts = [{"part": label, "minutes": by_part.get(label, 0)} for label, *_ in PARTS_OF_DAY]
        peak = max(parts, key=lambda p: p["minutes"]) if any(p["minutes"] for p in parts) else None
        return {
            "by_weekday": [
                {"weekday": i, "label": WEEKDAY_LABELS[i], "minutes": by_weekday[i]} for i in range(7)
            ],
            "by_part_of_day": parts,
            "peak_part_of_day": peak["part"] if peak else None,
        }

    # --- Velocity ---------------------------------------------------------

    def velocity(self) -> dict:
        """Recent lessons-completed-per-week and a finish projection at that actual pace."""
        total = len(self.stats.lessons)
        completed = self.stats.completed_ids()
        cutoff = self.today - timedelta(days=VELOCITY_WINDOW_DAYS - 1)
        recent = 0
        for row in self.stats.progress.values():
            if row.status == LessonStatus.COMPLETED and row.completed_at is not None:
                if local_date(row.completed_at, self.stats.tz) >= cutoff:
                    recent += 1
        remaining = total - len(completed)
        per_week = recent / (VELOCITY_WINDOW_DAYS / 7)
        weeks_left = math.ceil(remaining / per_week) if per_week > 0 and remaining > 0 else None
        return {
            "total_lessons": total,
            "completed_lessons": len(completed),
            "remaining_lessons": remaining,
            "completed_last_4_weeks": recent,
            "lessons_per_week": round(per_week, 1),
            "projected_weeks_to_finish": weeks_left,
            "projected_finish_date": (self.today + timedelta(weeks=weeks_left)).isoformat()
            if weeks_left
            else None,
        }

    # --- Quiz performance -------------------------------------------------

    def _quiz_attempts(self) -> list[QuizAttempt]:
        return list(
            self.stats.db.scalars(
                select(QuizAttempt)
                .where(QuizAttempt.user_id == self.stats.user.id)
                .order_by(QuizAttempt.created_at)
            )
        )

    def quiz_performance(self) -> dict:
        """Overall quiz scoring, its recent trend, and the weakest domains and objectives."""
        attempts = self._quiz_attempts()
        scores = [a.score for a in attempts]
        average = round(sum(scores) / len(scores)) if scores else None

        # Trend: later half's average minus the earlier half's (needs enough attempts to mean anything).
        trend = None
        if len(scores) >= 4:
            mid = len(scores) // 2
            earlier = scores[:mid]
            later = scores[mid:]
            trend = round(sum(later) / len(later)) - round(sum(earlier) / len(earlier))

        # Weakest areas, from each attempted lesson's best quiz score.
        cert_names = {c.id: c.name for c in self.stats.db.scalars(select(Certification))}
        domain_scores: dict[str, list[int]] = defaultdict(list)
        objective_scores: dict[str, list[int]] = defaultdict(list)
        objective_meta: dict[str, tuple[str, str]] = {}
        for course in self.stats.courses:
            for lesson in course.lessons:
                row = self.stats.progress.get(lesson.id)
                if row is None or row.best_quiz_score is None:
                    continue
                domain_scores[course.domain].append(row.best_quiz_score)
                for objective in lesson.objectives:
                    objective_scores[objective.id].append(row.best_quiz_score)
                    objective_meta[objective.id] = (objective.title, cert_names.get(objective.certification_id, ""))

        weakest_domains = sorted(
            (
                {"domain": domain, "average_score": round(sum(s) / len(s)), "lessons_attempted": len(s)}
                for domain, s in domain_scores.items()
            ),
            key=lambda d: (d["average_score"], d["domain"]),
        )[:5]
        weakest_objectives = sorted(
            (
                {
                    "id": oid,
                    "title": objective_meta[oid][0],
                    "certification": objective_meta[oid][1],
                    "average_score": round(sum(s) / len(s)),
                    "lessons_attempted": len(s),
                }
                for oid, s in objective_scores.items()
            ),
            key=lambda o: (o["average_score"], o["id"]),
        )[:5]

        return {
            "total_attempts": len(attempts),
            "average_score": average,
            "score_trend": trend,
            "weakest_domains": weakest_domains,
            "weakest_objectives": weakest_objectives,
        }

    # --- Payload ----------------------------------------------------------

    def analytics(self) -> dict:
        return {
            "study_trend": self.study_trend(),
            "study_patterns": self.study_patterns(),
            "velocity": self.velocity(),
            "quiz_performance": self.quiz_performance(),
        }
