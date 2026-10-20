"""Certification practice exams.

An exam samples quiz questions from across the lessons that map to a
certification's objectives, spread as evenly as the question bank allows, so the
result reflects breadth of readiness rather than one lesson. Grading reuses the
quiz logic and adds a per-objective breakdown.
"""

from __future__ import annotations

import random
from collections import defaultdict
from dataclasses import dataclass

from sqlalchemy import select
from sqlalchemy.orm import Session, selectinload

from ..models import CertObjective, Certification, Lesson, QuizQuestion

# Exams are graded harder than individual lessons: this mirrors a real exam cut score.
PASS_MARK = 80
DEFAULT_SIZE = 20
MIN_SIZE = 5
MAX_SIZE = 100


@dataclass(frozen=True)
class ObjectiveBreakdown:
    code: str
    title: str
    correct: int
    total: int

    @property
    def percent(self) -> int:
        return round(100 * self.correct / self.total) if self.total else 0


@dataclass(frozen=True)
class ExamResult:
    score: int
    correct: int
    total: int
    passed: bool
    breakdown: list[ObjectiveBreakdown]


def certification_questions(db: Session, cert_id: str) -> dict[int, list[CertObjective]]:
    """Every quiz question that belongs to a lesson mapped to this certification.

    Returns question id -> the objectives of *this* certification it exercises.
    """
    cert = db.get(Certification, cert_id)
    if cert is None:
        return {}
    lessons = db.scalars(
        select(Lesson)
        .options(selectinload(Lesson.objectives), selectinload(Lesson.questions))
        .join(Lesson.objectives)
        .where(CertObjective.certification_id == cert_id)
        .distinct()
    )
    result: dict[int, list[CertObjective]] = {}
    for lesson in lessons:
        cert_objectives = [o for o in lesson.objectives if o.certification_id == cert_id]
        for question in lesson.questions:
            # One question can appear via several lessons; keep the union of objectives.
            existing = result.setdefault(question.id, [])
            for objective in cert_objectives:
                if objective not in existing:
                    existing.append(objective)
    return result


def available_question_count(db: Session, cert_id: str) -> int:
    return len(certification_questions(db, cert_id))


def build_exam(db: Session, cert_id: str, size: int, rng: random.Random | None = None) -> list[QuizQuestion]:
    """Pick up to `size` questions, spread across objectives, newest objectives not favoured.

    Questions are grouped by the objective they cover and drawn round-robin so no
    single objective dominates a short exam; the order is then shuffled.
    """
    rng = rng or random.Random()
    question_objectives = certification_questions(db, cert_id)
    if not question_objectives:
        return []

    by_objective: dict[str, list[int]] = defaultdict(list)
    for qid, objectives in question_objectives.items():
        # Assign each question to its lowest objective code so the buckets are stable.
        key = min((o.code for o in objectives), default="")
        by_objective[key].append(qid)

    buckets = []
    for code in sorted(by_objective):
        ids = by_objective[code]
        rng.shuffle(ids)
        buckets.append(ids)

    chosen: list[int] = []
    target = min(size, len(question_objectives))
    while len(chosen) < target:
        progressed = False
        for bucket in buckets:
            if bucket:
                chosen.append(bucket.pop())
                progressed = True
                if len(chosen) >= target:
                    break
        if not progressed:
            break

    questions = {q.id: q for q in db.scalars(select(QuizQuestion).where(QuizQuestion.id.in_(chosen)))}
    ordered = [questions[qid] for qid in chosen if qid in questions]
    rng.shuffle(ordered)
    return ordered


def grade_exam(
    db: Session, cert_id: str, answers: dict[int, int | None]
) -> ExamResult:
    """Grade a map of question id -> selected index for a certification exam."""
    question_objectives = certification_questions(db, cert_id)
    questions = {q.id: q for q in db.scalars(select(QuizQuestion).where(QuizQuestion.id.in_(answers)))}

    per_objective_correct: dict[str, int] = defaultdict(int)
    per_objective_total: dict[str, int] = defaultdict(int)
    objective_titles: dict[str, str] = {}
    correct = 0

    for qid, selected in answers.items():
        question = questions.get(qid)
        if question is None:
            continue
        is_correct = selected is not None and selected == question.correct_index
        correct += int(is_correct)
        for objective in question_objectives.get(qid, []):
            objective_titles[objective.code] = objective.title
            per_objective_total[objective.code] += 1
            per_objective_correct[objective.code] += int(is_correct)

    total = len(answers)
    score = round(100 * correct / total) if total else 0
    breakdown = [
        ObjectiveBreakdown(
            code=code,
            title=objective_titles[code],
            correct=per_objective_correct[code],
            total=per_objective_total[code],
        )
        for code in sorted(per_objective_total)
    ]
    return ExamResult(score=score, correct=correct, total=total, passed=score >= PASS_MARK, breakdown=breakdown)
