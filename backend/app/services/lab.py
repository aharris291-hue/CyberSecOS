"""Graded lab marking.

A lab task has one of four answer types, each compared after normalising what the
learner typed, so harmless differences (case, spacing, a trailing full stop, quotes)
never cost a mark:

- ``text``: matches any of the accepted strings.
- ``number``: the same number as an accepted value (``1,204`` and ``1204`` agree), or
  within the task's ``tolerance`` when it has one (e.g. a beacon interval estimated from timestamps).
- ``choice``: the index of the correct option.
- ``set``: the same unordered collection of items, separated by commas, semicolons,
  spaces or new lines. Every item must be present and nothing extra.

Each task is right or wrong; the lab score is the percentage of tasks right.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

TASK_TYPES = ("text", "number", "choice", "set")
DEFAULT_PASS_MARK = 80  # percent
MAX_ANSWER_LENGTH = 2000

_SET_SEPARATORS = re.compile(r"[,;\s]+")


@dataclass(frozen=True)
class TaskResult:
    index: int
    is_correct: bool
    # Hint for a wrong answer; the explanation once the task is right or the lab is passed.
    feedback: str


@dataclass(frozen=True)
class LabResult:
    score: int
    correct: int
    total: int
    passed: bool
    results: list[TaskResult]


def normalise_text(value: str) -> str:
    value = " ".join(value.strip().lower().split())
    value = value.strip("\"'`")
    return value.rstrip(".").strip()


def _parse_number(value: str) -> float | None:
    try:
        return float(normalise_text(value).replace(",", ""))
    except ValueError:
        return None


def _set_items(value: str) -> frozenset[str]:
    return frozenset(item for item in (normalise_text(i) for i in _SET_SEPARATORS.split(value)) if item)


def is_correct(task: dict, answer: str | None) -> bool:
    if answer is None or not answer.strip():
        return False
    accepted = [str(a) for a in task["accepted"]]
    kind = task["type"]
    if kind == "text":
        return normalise_text(answer) in {normalise_text(a) for a in accepted}
    if kind == "number":
        given = _parse_number(answer)
        tolerance = float(task.get("tolerance", 0))
        return given is not None and any(
            (expected := _parse_number(a)) is not None and abs(given - expected) <= tolerance for a in accepted
        )
    if kind == "choice":
        return normalise_text(answer) in {normalise_text(a) for a in accepted}
    if kind == "set":
        return _set_items(answer) in {_set_items(a) for a in accepted}
    raise ValueError(f"Unknown lab task type {kind!r}")


def grade(tasks: list[dict], answers: list[str | None], pass_mark: int = DEFAULT_PASS_MARK) -> LabResult:
    """Grade answers given in task order. Blank answers count as wrong."""
    if len(answers) != len(tasks):
        raise ValueError(f"Expected {len(tasks)} answers, got {len(answers)}")
    marks = [is_correct(task, answer) for task, answer in zip(tasks, answers, strict=True)]
    correct = sum(marks)
    total = len(tasks)
    score = round(100 * correct / total) if total else 0
    passed = score >= pass_mark
    results = [
        TaskResult(
            index=i,
            is_correct=right,
            # Explanations would give the answer away, so a wrong task only gets its hint
            # until the learner has passed the lab.
            feedback=task["explanation"] if right or passed else task["hint"],
        )
        for i, (task, right) in enumerate(zip(tasks, marks, strict=True))
    ]
    return LabResult(score=score, correct=correct, total=total, passed=passed, results=results)
