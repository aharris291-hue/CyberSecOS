"""Course engine: certifications, courses, lessons and quizzes."""

from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.orm import Session, selectinload

from ..database import get_db
from ..models import Certification, Course, Lesson, LessonProgress, LessonStatus, QuizAttempt, User
from ..schemas import (
    CertificationOut,
    CourseDetail,
    CourseSummary,
    LessonDetail,
    LessonProgressOut,
    LessonSummary,
    ObjectiveOut,
    QuestionOut,
    QuizResultOut,
    QuizSubmission,
)
from ..security import UNPROCESSABLE, get_current_user
from ..services import quiz
from ..services.progress import progress_by_lesson, record_quiz_result

router = APIRouter(prefix="/api", tags=["courses"])


def _status(progress: LessonProgress | None) -> str:
    return progress.status if progress else LessonStatus.NOT_STARTED


def _lesson_summary(lesson: Lesson, progress: LessonProgress | None) -> LessonSummary:
    return LessonSummary(
        id=lesson.id,
        slug=lesson.slug,
        title=lesson.title,
        order=lesson.order,
        summary=lesson.summary,
        difficulty=lesson.difficulty,
        theory_minutes=lesson.theory_minutes,
        lab_minutes=lesson.lab_minutes,
        status=_status(progress),
    )


def _course_summary(course: Course, progress: dict[int, LessonProgress]) -> dict:
    return dict(
        id=course.id,
        slug=course.slug,
        title=course.title,
        domain=course.domain,
        description=course.description,
        order=course.order,
        lesson_count=len(course.lessons),
        completed_lessons=sum(_status(progress.get(l.id)) == LessonStatus.COMPLETED for l in course.lessons),
        total_minutes=sum(l.theory_minutes + l.lab_minutes for l in course.lessons),
    )


def progress_out(progress: LessonProgress | None) -> LessonProgressOut:
    if progress is None:
        return LessonProgressOut()
    return LessonProgressOut(
        status=progress.status,
        theory_completed=progress.theory_completed_at is not None,
        lab_completed=progress.lab_completed_at is not None,
        best_quiz_score=progress.best_quiz_score,
        quiz_passed=progress.quiz_passed_at is not None,
        lab_notes=progress.lab_notes,
        completed_at=progress.completed_at,
    )


def get_lesson_or_404(db: Session, lesson_id: int) -> Lesson:
    lesson = db.get(Lesson, lesson_id)
    if lesson is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "Lesson not found")
    return lesson


@router.get("/certifications", response_model=list[CertificationOut])
def list_certifications(db: Session = Depends(get_db), _: User = Depends(get_current_user)) -> list[Certification]:
    return list(db.scalars(select(Certification).order_by(Certification.priority)))


@router.get("/courses", response_model=list[CourseSummary])
def list_courses(db: Session = Depends(get_db), user: User = Depends(get_current_user)) -> list[CourseSummary]:
    courses = db.scalars(select(Course).options(selectinload(Course.lessons)).order_by(Course.order))
    progress = progress_by_lesson(db, user)
    return [CourseSummary(**_course_summary(course, progress)) for course in courses]


@router.get("/courses/{slug}", response_model=CourseDetail)
def read_course(slug: str, db: Session = Depends(get_db), user: User = Depends(get_current_user)) -> CourseDetail:
    course = db.scalar(select(Course).where(Course.slug == slug).options(selectinload(Course.lessons)))
    if course is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "Course not found")
    progress = progress_by_lesson(db, user)
    return CourseDetail(
        **_course_summary(course, progress),
        lessons=[_lesson_summary(lesson, progress.get(lesson.id)) for lesson in course.lessons],
    )


@router.get("/lessons/{lesson_id}", response_model=LessonDetail)
def read_lesson(lesson_id: int, db: Session = Depends(get_db), user: User = Depends(get_current_user)) -> LessonDetail:
    lesson = get_lesson_or_404(db, lesson_id)
    progress = progress_by_lesson(db, user).get(lesson.id)
    siblings = lesson.course.lessons
    index = siblings.index(lesson)
    return LessonDetail(
        **_lesson_summary(lesson, progress).model_dump(),
        course_slug=lesson.course.slug,
        course_title=lesson.course.title,
        learning_objectives=lesson.learning_objectives,
        theory=lesson.theory,
        examples=lesson.examples,
        exercise=lesson.exercise,
        expected_outcome=lesson.expected_outcome,
        questions=[QuestionOut.model_validate(q) for q in lesson.questions],
        certification_objectives=[ObjectiveOut.model_validate(o) for o in lesson.objectives],
        progress=progress_out(progress),
        previous_lesson_id=siblings[index - 1].id if index > 0 else None,
        next_lesson_id=siblings[index + 1].id if index + 1 < len(siblings) else None,
        has_graded_lab=lesson.lab is not None,
    )


@router.post("/lessons/{lesson_id}/quiz", response_model=QuizResultOut)
def submit_quiz(
    lesson_id: int, body: QuizSubmission, db: Session = Depends(get_db), user: User = Depends(get_current_user)
) -> QuizResultOut:
    lesson = get_lesson_or_404(db, lesson_id)
    try:
        result = quiz.grade(lesson.questions, body.answers)
    except ValueError as exc:
        raise HTTPException(UNPROCESSABLE, str(exc)) from None

    db.add(
        QuizAttempt(
            user_id=user.id,
            lesson_id=lesson.id,
            score=result.score,
            correct=result.correct,
            total=result.total,
            answers=body.answers,
        )
    )
    progress = record_quiz_result(db, user, lesson, result)
    db.commit()
    return QuizResultOut(
        score=result.score,
        correct=result.correct,
        total=result.total,
        passed=result.passed,
        pass_mark=quiz.PASS_MARK,
        results=[r.__dict__ for r in result.results],
        lesson_status=progress.status,
    )
