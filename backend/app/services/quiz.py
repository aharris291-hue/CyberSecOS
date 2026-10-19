"""Quiz grading."""

from __future__ import annotations

from dataclasses import dataclass

from ..models import QuizQuestion

PASS_MARK = 70  # percent


@dataclass(frozen=True)
class QuestionResult:
    question_id: int
    selected_index: int | None
    correct_index: int
    is_correct: bool
    explanation: str


@dataclass(frozen=True)
class QuizResult:
    score: int
    correct: int
    total: int
    passed: bool
    results: list[QuestionResult]


def grade(questions: list[QuizQuestion], answers: list[int | None]) -> QuizResult:
    """Grade answers given in question order. Missing or out-of-range answers count as wrong."""
    if len(answers) != len(questions):
        raise ValueError(f"Expected {len(questions)} answers, got {len(answers)}")
    results = []
    for question, selected in zip(questions, answers, strict=True):
        is_correct = selected is not None and selected == question.correct_index
        results.append(
            QuestionResult(
                question_id=question.id,
                selected_index=selected,
                correct_index=question.correct_index,
                is_correct=is_correct,
                explanation=question.explanation,
            )
        )
    correct = sum(result.is_correct for result in results)
    total = len(questions)
    score = round(100 * correct / total) if total else 0
    return QuizResult(score=score, correct=correct, total=total, passed=score >= PASS_MARK, results=results)
