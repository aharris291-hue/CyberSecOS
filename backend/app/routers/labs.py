"""Graded labs: the scenario and data to analyse, answer submission and automatic marking."""

from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, Response, status
from sqlalchemy import func, select
from sqlalchemy.orm import Session, selectinload

from ..database import get_db
from ..models import Course, Lab, LabAttempt, Lesson, User
from ..schemas import (
    LabArtifactOut,
    LabOut,
    LabResultOut,
    LabStatusOut,
    LabSubmission,
    LabSummaryOut,
    LabTaskOut,
)
from ..security import UNPROCESSABLE, get_current_user
from ..services import lab as lab_service
from ..services.progress import record_lab_result
from .courses import get_lesson_or_404

router = APIRouter(prefix="/api", tags=["labs"])


def _get_lab_or_404(db: Session, lesson_id: int) -> Lab:
    lab = get_lesson_or_404(db, lesson_id).lab
    if lab is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "This lesson has no graded lab")
    return lab


def lab_statuses(db: Session, user: User) -> dict[int, LabStatusOut]:
    """Attempts, best score and pass state per lab for one learner."""
    rows = db.execute(
        select(LabAttempt.lab_id, func.count(), func.max(LabAttempt.score), func.max(LabAttempt.passed))
        .where(LabAttempt.user_id == user.id)
        .group_by(LabAttempt.lab_id)
    )
    return {
        lab_id: LabStatusOut(attempts=count, best_score=best, passed=bool(passed))
        for lab_id, count, best, passed in rows
    }


@router.get("/labs", response_model=list[LabSummaryOut])
def list_labs(db: Session = Depends(get_db), user: User = Depends(get_current_user)) -> list[LabSummaryOut]:
    labs = db.scalars(
        select(Lab)
        .join(Lab.lesson)
        .join(Lesson.course)
        .options(selectinload(Lab.lesson).selectinload(Lesson.course))
        .order_by(Course.order, Lesson.order)
    )
    statuses = lab_statuses(db, user)
    return [
        LabSummaryOut(
            id=lab.id,
            slug=lab.slug,
            title=lab.title,
            lesson_id=lab.lesson_id,
            lesson_title=lab.lesson.title,
            course_slug=lab.lesson.course.slug,
            course_title=lab.lesson.course.title,
            task_count=len(lab.tasks),
            pass_mark=lab.pass_mark,
            status=statuses.get(lab.id, LabStatusOut()),
        )
        for lab in labs
    ]


@router.get("/lessons/{lesson_id}/lab", response_model=LabOut)
def read_lab(lesson_id: int, db: Session = Depends(get_db), user: User = Depends(get_current_user)) -> LabOut:
    lab = _get_lab_or_404(db, lesson_id)
    return LabOut(
        id=lab.id,
        slug=lab.slug,
        title=lab.title,
        lesson_id=lab.lesson_id,
        scenario=lab.scenario,
        pass_mark=lab.pass_mark,
        artifacts=[LabArtifactOut(**artifact) for artifact in lab.artifacts],
        tasks=[
            LabTaskOut(index=i, prompt=task["prompt"], type=task["type"], options=task["options"])
            for i, task in enumerate(lab.tasks)
        ],
        status=lab_statuses(db, user).get(lab.id, LabStatusOut()),
    )


@router.get(
    "/lessons/{lesson_id}/lab/artifacts/{name}",
    responses={200: {"content": {"text/plain": {}}, "description": "The artifact as a file"}},
)
def download_artifact(
    lesson_id: int, name: str, db: Session = Depends(get_db), _: User = Depends(get_current_user)
) -> Response:
    """Download one lab file, so it can be worked on with grep, awk or a spreadsheet."""
    lab = _get_lab_or_404(db, lesson_id)
    artifact = next((a for a in lab.artifacts if a["name"] == name), None)
    if artifact is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "Artifact not found")
    return Response(
        content=artifact["content"],
        media_type="text/plain; charset=utf-8",
        headers={"Content-Disposition": f'attachment; filename="{artifact["name"]}"'},
    )


@router.post("/lessons/{lesson_id}/lab/attempts", response_model=LabResultOut)
def submit_lab(
    lesson_id: int, body: LabSubmission, db: Session = Depends(get_db), user: User = Depends(get_current_user)
) -> LabResultOut:
    lab = _get_lab_or_404(db, lesson_id)
    if any(answer is not None and len(answer) > lab_service.MAX_ANSWER_LENGTH for answer in body.answers):
        raise HTTPException(UNPROCESSABLE, f"Answers are limited to {lab_service.MAX_ANSWER_LENGTH} characters")
    try:
        result = lab_service.grade(lab.tasks, body.answers, lab.pass_mark)
    except ValueError as exc:
        raise HTTPException(UNPROCESSABLE, str(exc)) from None

    db.add(
        LabAttempt(
            user_id=user.id,
            lab_id=lab.id,
            score=result.score,
            correct=result.correct,
            total=result.total,
            passed=result.passed,
            answers=body.answers,
        )
    )
    progress = record_lab_result(db, user, lab.lesson, result)
    db.commit()
    return LabResultOut(
        score=result.score,
        correct=result.correct,
        total=result.total,
        passed=result.passed,
        pass_mark=lab.pass_mark,
        results=[r.__dict__ for r in result.results],
        lab_completed=progress.lab_completed_at is not None,
        lesson_status=progress.status,
    )
