"""Load bundled course content and the certification catalog into the database.

Seeding is idempotent: rows are matched by slug/id and updated in place, so a
content change ships with the next app start without touching learner progress.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml
from sqlalchemy.orm import Session

from ..models import CertObjective, Certification, Course, Lab, Lesson, QuizQuestion
from .lab import DEFAULT_PASS_MARK, TASK_TYPES

SEED_DIR = Path(__file__).resolve().parent.parent / "seed"


class ContentError(ValueError):
    """Bundled content is malformed. Raised at startup so bad content never ships silently."""


def load_yaml(path: Path) -> Any:
    with path.open(encoding="utf-8") as handle:
        return yaml.safe_load(handle)


def load_course_files(seed_dir: Path = SEED_DIR) -> list[dict]:
    return [load_yaml(path) for path in sorted((seed_dir / "content").glob("*.yaml"))]


def load_lab_files(seed_dir: Path = SEED_DIR) -> list[dict]:
    return [load_yaml(path) for path in sorted((seed_dir / "labs").glob("*.yaml"))]


def seed_database(session: Session, seed_dir: Path = SEED_DIR) -> None:
    _seed_certifications(session, load_yaml(seed_dir / "certifications.yaml"))
    session.flush()
    for course_data in load_course_files(seed_dir):
        _seed_course(session, course_data)
    session.flush()
    for lab_data in load_lab_files(seed_dir):
        _seed_lab(session, lab_data)
    session.commit()


def _seed_certifications(session: Session, certifications: list[dict]) -> None:
    for data in certifications:
        cert = session.get(Certification, data["id"]) or Certification(id=data["id"])
        cert.name = data["name"]
        cert.exam_code = data["exam_code"]
        cert.priority = data["priority"]
        session.add(cert)
        for objective in data["objectives"]:
            objective_id = f"{cert.id}:{objective['code']}"
            row = session.get(CertObjective, objective_id) or CertObjective(id=objective_id, certification_id=cert.id)
            row.code = str(objective["code"])
            row.title = objective["title"]
            session.add(row)


def _seed_course(session: Session, data: dict) -> None:
    course = session.query(Course).filter_by(slug=data["slug"]).one_or_none() or Course(slug=data["slug"])
    course.title = data["title"]
    course.domain = data["domain"]
    course.description = data["description"].strip()
    course.order = data["order"]
    session.add(course)

    for lesson_data in data["lessons"]:
        _seed_lesson(session, course, lesson_data)


def _seed_lesson(session: Session, course: Course, data: dict) -> None:
    slug = data["slug"]
    lesson = session.query(Lesson).filter_by(slug=slug).one_or_none() or Lesson(slug=slug)
    lesson.course = course
    lesson.title = data["title"]
    lesson.order = data["order"]
    lesson.summary = data["summary"]
    lesson.difficulty = data.get("difficulty", "beginner")
    lesson.learning_objectives = list(data["learning_objectives"])
    lesson.theory = data["theory"].strip()
    lesson.examples = list(data["examples"])
    lesson.exercise = dict(data["exercise"])
    lesson.expected_outcome = data["expected_outcome"].strip()
    lesson.theory_minutes = data["theory_minutes"]
    lesson.lab_minutes = data["lab_minutes"]

    objectives = []
    for objective_id in data.get("objectives", []):
        objective = session.get(CertObjective, objective_id)
        if objective is None:
            raise ContentError(f"Lesson {slug} references unknown objective {objective_id}")
        objectives.append(objective)
    lesson.objectives = objectives

    lesson.questions.clear()
    for index, question in enumerate(data["quiz"], start=1):
        options = [str(option) for option in question["options"]]
        if not 0 <= question["answer"] < len(options):
            raise ContentError(f"Lesson {slug} question {index} has an answer outside its options")
        lesson.questions.append(
            QuizQuestion(
                order=index,
                prompt=question["prompt"],
                options=options,
                correct_index=question["answer"],
                explanation=question["explanation"],
            )
        )
    session.add(lesson)


def lab_tasks(data: dict) -> list[dict]:
    """Validate a lab's tasks and return them in the stored shape (answer key as strings)."""
    slug = data["slug"]
    tasks = []
    for index, task in enumerate(data["tasks"], start=1):
        kind = task.get("type", "text")
        if kind not in TASK_TYPES:
            raise ContentError(f"Lab {slug} task {index} has unknown type {kind!r}")
        options = [str(option) for option in task.get("options", [])]
        if kind == "choice":
            if not 0 <= task.get("answer", -1) < len(options):
                raise ContentError(f"Lab {slug} task {index} has an answer outside its options")
            accepted = [str(task["answer"])]
        else:
            if options:
                raise ContentError(f"Lab {slug} task {index} has options but is not a choice task")
            accepted = [str(value) for value in task.get("accepted", [])]
        if task.get("tolerance") is not None and kind != "number":
            raise ContentError(f"Lab {slug} task {index} has a tolerance but is not a number task")
        if not accepted:
            raise ContentError(f"Lab {slug} task {index} has no accepted answer")
        if not (task.get("hint") or "").strip() or not (task.get("explanation") or "").strip():
            raise ContentError(f"Lab {slug} task {index} needs a hint and an explanation")
        tasks.append(
            {
                "prompt": task["prompt"].strip(),
                "type": kind,
                "options": options,
                "accepted": accepted,
                "tolerance": float(task.get("tolerance", 0)),
                "hint": task["hint"].strip(),
                "explanation": task["explanation"].strip(),
            }
        )
    return tasks


def _seed_lab(session: Session, data: dict) -> None:
    slug = data["slug"]
    lesson = session.query(Lesson).filter_by(slug=data["lesson"]).one_or_none()
    if lesson is None:
        raise ContentError(f"Lab {slug} references unknown lesson {data['lesson']}")
    names = [artifact["name"] for artifact in data["artifacts"]]
    if not names or len(names) != len(set(names)):
        raise ContentError(f"Lab {slug} needs at least one artifact and unique artifact names")

    lab = session.query(Lab).filter_by(slug=slug).one_or_none() or Lab(slug=slug)
    if lesson.lab is not None and lesson.lab is not lab:
        raise ContentError(f"Lesson {lesson.slug} already has lab {lesson.lab.slug}")
    lab.lesson = lesson
    lab.title = data["title"]
    lab.scenario = data["scenario"].strip()
    lab.pass_mark = data.get("pass_mark", DEFAULT_PASS_MARK)
    lab.artifacts = [
        {
            "name": artifact["name"],
            "kind": artifact.get("kind", "text"),
            "description": artifact.get("description", "").strip(),
            "content": artifact["content"],
        }
        for artifact in data["artifacts"]
    ]
    lab.tasks = lab_tasks(data)
    session.add(lab)
