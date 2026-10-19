"""Load bundled course content and the certification catalog into the database.

Seeding is idempotent: rows are matched by slug/id and updated in place, so a
content change ships with the next app start without touching learner progress.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml
from sqlalchemy.orm import Session

from ..models import CertObjective, Certification, Course, Lesson, QuizQuestion

SEED_DIR = Path(__file__).resolve().parent.parent / "seed"


class ContentError(ValueError):
    """Bundled content is malformed. Raised at startup so bad content never ships silently."""


def load_yaml(path: Path) -> Any:
    with path.open(encoding="utf-8") as handle:
        return yaml.safe_load(handle)


def load_course_files(seed_dir: Path = SEED_DIR) -> list[dict]:
    return [load_yaml(path) for path in sorted((seed_dir / "content").glob("*.yaml"))]


def seed_database(session: Session, seed_dir: Path = SEED_DIR) -> None:
    _seed_certifications(session, load_yaml(seed_dir / "certifications.yaml"))
    session.flush()
    for course_data in load_course_files(seed_dir):
        _seed_course(session, course_data)
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
