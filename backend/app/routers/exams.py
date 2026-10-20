"""Certification practice exam endpoints."""

from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, Query, status
from sqlalchemy import select
from sqlalchemy.orm import Session

from datetime import timedelta

from ..database import get_db, utcnow
from ..models import Certification, ExamAttempt, ExamTarget, User
from ..schemas import (
    ExamAttemptOut,
    ExamInfo,
    ExamPaper,
    ExamResultOut,
    ExamSubmission,
    ExamTargetOut,
    ExamTargetUpdate,
    QuestionOut,
)
from ..security import UNPROCESSABLE, get_current_user
from ..services import exam as exam_service

router = APIRouter(prefix="/api/certifications/{cert_id}/exam", tags=["exams"])


def _get_cert_or_404(db: Session, cert_id: str) -> Certification:
    cert = db.get(Certification, cert_id)
    if cert is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "Certification not found")
    return cert


@router.get("", response_model=ExamInfo)
def exam_info(cert_id: str, db: Session = Depends(get_db), _: User = Depends(get_current_user)) -> ExamInfo:
    cert = _get_cert_or_404(db, cert_id)
    return ExamInfo(
        certification_id=cert.id,
        name=cert.name,
        exam_code=cert.exam_code,
        available_questions=exam_service.available_question_count(db, cert_id),
        default_size=exam_service.DEFAULT_SIZE,
        pass_mark=exam_service.PASS_MARK,
    )


@router.get("/paper", response_model=ExamPaper)
def exam_paper(
    cert_id: str,
    size: int = Query(default=exam_service.DEFAULT_SIZE, ge=exam_service.MIN_SIZE, le=exam_service.MAX_SIZE),
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> ExamPaper:
    cert = _get_cert_or_404(db, cert_id)
    questions = exam_service.build_exam(db, cert_id, size)
    if not questions:
        raise HTTPException(UNPROCESSABLE, "No questions are available for this certification yet")
    return ExamPaper(
        certification_id=cert.id,
        name=cert.name,
        exam_code=cert.exam_code,
        pass_mark=exam_service.PASS_MARK,
        questions=[QuestionOut.model_validate(q) for q in questions],
    )


@router.post("", response_model=ExamResultOut)
def submit_exam(
    cert_id: str, body: ExamSubmission, db: Session = Depends(get_db), user: User = Depends(get_current_user)
) -> ExamResultOut:
    _get_cert_or_404(db, cert_id)
    answers = {a.question_id: a.selected_index for a in body.answers}
    if len(answers) != len(body.answers):
        raise HTTPException(UNPROCESSABLE, "Duplicate question in submission")

    result = exam_service.grade_exam(db, cert_id, answers)
    if result.total == 0:
        raise HTTPException(UNPROCESSABLE, "No gradable questions in submission")

    db.add(
        ExamAttempt(
            user_id=user.id,
            certification_id=cert_id,
            score=result.score,
            correct=result.correct,
            total=result.total,
            passed=result.passed,
            duration_seconds=body.duration_seconds,
            breakdown=[b.__dict__ | {"percent": b.percent} for b in result.breakdown],
        )
    )
    db.commit()
    return ExamResultOut(
        certification_id=cert_id,
        score=result.score,
        correct=result.correct,
        total=result.total,
        passed=result.passed,
        pass_mark=exam_service.PASS_MARK,
        breakdown=[
            {"code": b.code, "title": b.title, "correct": b.correct, "total": b.total, "percent": b.percent}
            for b in result.breakdown
        ],
    )


@router.get("/history", response_model=list[ExamAttemptOut])
def exam_history(
    cert_id: str,
    limit: int = Query(default=20, ge=1, le=100),
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> list[ExamAttempt]:
    _get_cert_or_404(db, cert_id)
    return list(
        db.scalars(
            select(ExamAttempt)
            .where(ExamAttempt.user_id == user.id, ExamAttempt.certification_id == cert_id)
            .order_by(ExamAttempt.created_at.desc())
            .limit(limit)
        )
    )

def _local_today(tz_offset_minutes: int):
    return (utcnow() + timedelta(minutes=tz_offset_minutes)).date()


@router.get("/target", response_model=ExamTargetOut | None)
def get_exam_target(
    cert_id: str,
    tz_offset_minutes: int = 0,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> ExamTargetOut | None:
    _get_cert_or_404(db, cert_id)
    target = db.scalar(
        select(ExamTarget).where(ExamTarget.user_id == user.id, ExamTarget.certification_id == cert_id)
    )
    if target is None:
        return None
    return ExamTargetOut(
        certification_id=cert_id,
        exam_date=target.exam_date,
        days_until_exam=(target.exam_date - _local_today(tz_offset_minutes)).days,
    )


@router.put("/target", response_model=ExamTargetOut)
def set_exam_target(
    cert_id: str,
    body: ExamTargetUpdate,
    tz_offset_minutes: int = 0,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> ExamTargetOut:
    _get_cert_or_404(db, cert_id)
    target = db.scalar(
        select(ExamTarget).where(ExamTarget.user_id == user.id, ExamTarget.certification_id == cert_id)
    )
    if target is None:
        target = ExamTarget(user_id=user.id, certification_id=cert_id, exam_date=body.exam_date)
        db.add(target)
    else:
        target.exam_date = body.exam_date
    db.commit()
    return ExamTargetOut(
        certification_id=cert_id,
        exam_date=body.exam_date,
        days_until_exam=(body.exam_date - _local_today(tz_offset_minutes)).days,
    )


@router.delete("/target", status_code=status.HTTP_204_NO_CONTENT)
def clear_exam_target(
    cert_id: str,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> None:
    _get_cert_or_404(db, cert_id)
    target = db.scalar(
        select(ExamTarget).where(ExamTarget.user_id == user.id, ExamTarget.certification_id == cert_id)
    )
    if target is not None:
        db.delete(target)
        db.commit()

