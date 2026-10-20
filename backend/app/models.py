"""Database models.

Content (certifications, courses, lessons, quizzes) is seeded from YAML and
treated as read-only. Learner state (users, progress, attempts, activity) is
written by the API.
"""

from __future__ import annotations

from datetime import datetime
from enum import StrEnum

from sqlalchemy import JSON, Column, DateTime, ForeignKey, Integer, String, Table, Text, UniqueConstraint
from sqlalchemy.orm import Mapped, mapped_column, relationship

from .database import Base, utcnow


class LessonStatus(StrEnum):
    NOT_STARTED = "not_started"
    IN_PROGRESS = "in_progress"
    COMPLETED = "completed"


class ActivityKind(StrEnum):
    THEORY = "theory"
    LAB = "lab"
    QUIZ = "quiz"


class User(Base):
    __tablename__ = "users"

    id: Mapped[int] = mapped_column(primary_key=True)
    username: Mapped[str] = mapped_column(String(64), unique=True, index=True)
    password_hash: Mapped[str] = mapped_column(String(128))
    display_name: Mapped[str] = mapped_column(String(100))
    target_role: Mapped[str] = mapped_column(String(100), default="SOC Analyst")
    weekly_goal_hours: Mapped[int] = mapped_column(Integer, default=10)
    failed_login_attempts: Mapped[int] = mapped_column(Integer, default=0)
    locked_until: Mapped[datetime | None] = mapped_column(DateTime, nullable=True)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=utcnow)
    last_login_at: Mapped[datetime | None] = mapped_column(DateTime, nullable=True)

    progress: Mapped[list[LessonProgress]] = relationship(back_populates="user", cascade="all, delete-orphan")


class Certification(Base):
    __tablename__ = "certifications"

    id: Mapped[str] = mapped_column(String(32), primary_key=True)  # e.g. "secplus"
    name: Mapped[str] = mapped_column(String(100))
    exam_code: Mapped[str] = mapped_column(String(32))
    priority: Mapped[int] = mapped_column(Integer)

    objectives: Mapped[list[CertObjective]] = relationship(
        back_populates="certification", cascade="all, delete-orphan", order_by="CertObjective.code"
    )


lesson_objectives = Table(
    "lesson_objectives",
    Base.metadata,
    Column("lesson_id", ForeignKey("lessons.id", ondelete="CASCADE"), primary_key=True),
    Column("objective_id", ForeignKey("cert_objectives.id", ondelete="CASCADE"), primary_key=True),
)


class CertObjective(Base):
    __tablename__ = "cert_objectives"

    id: Mapped[str] = mapped_column(String(64), primary_key=True)  # e.g. "secplus:4.9"
    certification_id: Mapped[str] = mapped_column(ForeignKey("certifications.id", ondelete="CASCADE"))
    code: Mapped[str] = mapped_column(String(16))
    title: Mapped[str] = mapped_column(String(255))

    certification: Mapped[Certification] = relationship(back_populates="objectives")
    lessons: Mapped[list[Lesson]] = relationship(secondary=lesson_objectives, back_populates="objectives")


class Course(Base):
    __tablename__ = "courses"

    id: Mapped[int] = mapped_column(primary_key=True)
    slug: Mapped[str] = mapped_column(String(64), unique=True)
    title: Mapped[str] = mapped_column(String(150))
    domain: Mapped[str] = mapped_column(String(64))
    description: Mapped[str] = mapped_column(Text)
    order: Mapped[int] = mapped_column(Integer)

    lessons: Mapped[list[Lesson]] = relationship(
        back_populates="course", cascade="all, delete-orphan", order_by="Lesson.order"
    )


class Lesson(Base):
    """A lesson carries every section required by the education standards."""

    __tablename__ = "lessons"

    id: Mapped[int] = mapped_column(primary_key=True)
    slug: Mapped[str] = mapped_column(String(64), unique=True)
    course_id: Mapped[int] = mapped_column(ForeignKey("courses.id", ondelete="CASCADE"))
    title: Mapped[str] = mapped_column(String(150))
    order: Mapped[int] = mapped_column(Integer)
    summary: Mapped[str] = mapped_column(Text)
    difficulty: Mapped[str] = mapped_column(String(16), default="beginner")
    learning_objectives: Mapped[list[str]] = mapped_column(JSON)
    theory: Mapped[str] = mapped_column(Text)
    examples: Mapped[list[dict]] = mapped_column(JSON)
    exercise: Mapped[dict] = mapped_column(JSON)
    expected_outcome: Mapped[str] = mapped_column(Text)
    theory_minutes: Mapped[int] = mapped_column(Integer)
    lab_minutes: Mapped[int] = mapped_column(Integer)

    course: Mapped[Course] = relationship(back_populates="lessons")
    questions: Mapped[list[QuizQuestion]] = relationship(
        back_populates="lesson", cascade="all, delete-orphan", order_by="QuizQuestion.order"
    )
    objectives: Mapped[list[CertObjective]] = relationship(secondary=lesson_objectives, back_populates="lessons")


class QuizQuestion(Base):
    __tablename__ = "quiz_questions"

    id: Mapped[int] = mapped_column(primary_key=True)
    lesson_id: Mapped[int] = mapped_column(ForeignKey("lessons.id", ondelete="CASCADE"))
    order: Mapped[int] = mapped_column(Integer)
    prompt: Mapped[str] = mapped_column(Text)
    options: Mapped[list[str]] = mapped_column(JSON)
    correct_index: Mapped[int] = mapped_column(Integer)
    explanation: Mapped[str] = mapped_column(Text)

    lesson: Mapped[Lesson] = relationship(back_populates="questions")


class LessonProgress(Base):
    __tablename__ = "lesson_progress"
    __table_args__ = (UniqueConstraint("user_id", "lesson_id"),)

    id: Mapped[int] = mapped_column(primary_key=True)
    user_id: Mapped[int] = mapped_column(ForeignKey("users.id", ondelete="CASCADE"))
    lesson_id: Mapped[int] = mapped_column(ForeignKey("lessons.id", ondelete="CASCADE"))
    status: Mapped[str] = mapped_column(String(16), default=LessonStatus.NOT_STARTED)
    started_at: Mapped[datetime] = mapped_column(DateTime, default=utcnow)
    theory_completed_at: Mapped[datetime | None] = mapped_column(DateTime, nullable=True)
    lab_completed_at: Mapped[datetime | None] = mapped_column(DateTime, nullable=True)
    lab_notes: Mapped[str | None] = mapped_column(Text, nullable=True)
    best_quiz_score: Mapped[int | None] = mapped_column(Integer, nullable=True)
    quiz_passed_at: Mapped[datetime | None] = mapped_column(DateTime, nullable=True)
    completed_at: Mapped[datetime | None] = mapped_column(DateTime, nullable=True)

    user: Mapped[User] = relationship(back_populates="progress")
    lesson: Mapped[Lesson] = relationship()


class QuizAttempt(Base):
    __tablename__ = "quiz_attempts"

    id: Mapped[int] = mapped_column(primary_key=True)
    user_id: Mapped[int] = mapped_column(ForeignKey("users.id", ondelete="CASCADE"), index=True)
    lesson_id: Mapped[int] = mapped_column(ForeignKey("lessons.id", ondelete="CASCADE"))
    score: Mapped[int] = mapped_column(Integer)  # percentage 0-100
    correct: Mapped[int] = mapped_column(Integer)
    total: Mapped[int] = mapped_column(Integer)
    answers: Mapped[list[int]] = mapped_column(JSON)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=utcnow)


class ActivityLog(Base):
    """Study time records. Drives streaks, weekly hours and the theory/lab split."""

    __tablename__ = "activity_log"

    id: Mapped[int] = mapped_column(primary_key=True)
    user_id: Mapped[int] = mapped_column(ForeignKey("users.id", ondelete="CASCADE"), index=True)
    lesson_id: Mapped[int | None] = mapped_column(ForeignKey("lessons.id", ondelete="SET NULL"), nullable=True)
    kind: Mapped[str] = mapped_column(String(16))
    minutes: Mapped[int] = mapped_column(Integer, default=0)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=utcnow, index=True)


class ExamAttempt(Base):
    """A completed certification practice exam. Breakdown is per-objective stats as JSON."""

    __tablename__ = "exam_attempts"

    id: Mapped[int] = mapped_column(primary_key=True)
    user_id: Mapped[int] = mapped_column(ForeignKey("users.id", ondelete="CASCADE"), index=True)
    certification_id: Mapped[str] = mapped_column(ForeignKey("certifications.id", ondelete="CASCADE"))
    score: Mapped[int] = mapped_column(Integer)  # percentage 0-100
    correct: Mapped[int] = mapped_column(Integer)
    total: Mapped[int] = mapped_column(Integer)
    passed: Mapped[bool] = mapped_column(Integer)  # SQLite stores as 0/1
    duration_seconds: Mapped[int | None] = mapped_column(Integer, nullable=True)
    breakdown: Mapped[list[dict]] = mapped_column(JSON)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=utcnow, index=True)
