"""Pydantic request and response models."""

from __future__ import annotations

from datetime import datetime

from pydantic import BaseModel, ConfigDict, Field


class ORMModel(BaseModel):
    model_config = ConfigDict(from_attributes=True)


# --- Auth & users ---------------------------------------------------------


class AuthStatus(BaseModel):
    registered: bool


class RegisterRequest(BaseModel):
    username: str = Field(min_length=3, max_length=64, pattern=r"^[A-Za-z0-9_.-]+$")
    password: str = Field(min_length=1, max_length=128)
    display_name: str = Field(min_length=1, max_length=100)


class LoginRequest(BaseModel):
    username: str = Field(min_length=1, max_length=64)
    password: str = Field(min_length=1, max_length=128)


class UserOut(ORMModel):
    id: int
    username: str
    display_name: str
    target_role: str
    weekly_goal_hours: int
    created_at: datetime
    last_login_at: datetime | None


class TokenResponse(BaseModel):
    access_token: str
    token_type: str = "bearer"
    expires_in: int
    user: UserOut


class ProfileUpdate(BaseModel):
    display_name: str | None = Field(default=None, min_length=1, max_length=100)
    target_role: str | None = Field(default=None, min_length=1, max_length=100)
    weekly_goal_hours: int | None = Field(default=None, ge=1, le=80)


class PasswordChange(BaseModel):
    current_password: str = Field(min_length=1, max_length=128)
    new_password: str = Field(min_length=1, max_length=128)


# --- Content ---------------------------------------------------------------


class ObjectiveOut(ORMModel):
    id: str
    certification_id: str
    code: str
    title: str


class CertificationOut(ORMModel):
    id: str
    name: str
    exam_code: str
    priority: int
    objectives: list[ObjectiveOut]


class Example(BaseModel):
    title: str
    body: str


class Exercise(BaseModel):
    title: str
    environment: str
    tools: list[str] = []
    steps: list[str]
    deliverable: str


class QuestionOut(ORMModel):
    """A quiz question as shown to the learner, without the answer."""

    id: int
    order: int
    prompt: str
    options: list[str]


class LessonProgressOut(BaseModel):
    status: str = "not_started"
    theory_completed: bool = False
    lab_completed: bool = False
    best_quiz_score: int | None = None
    quiz_passed: bool = False
    lab_notes: str | None = None
    completed_at: datetime | None = None


class LessonSummary(BaseModel):
    id: int
    slug: str
    title: str
    order: int
    summary: str
    difficulty: str
    theory_minutes: int
    lab_minutes: int
    status: str = "not_started"


class LessonDetail(LessonSummary):
    course_slug: str
    course_title: str
    learning_objectives: list[str]
    theory: str
    examples: list[Example]
    exercise: Exercise
    expected_outcome: str
    questions: list[QuestionOut]
    certification_objectives: list[ObjectiveOut]
    progress: LessonProgressOut
    previous_lesson_id: int | None = None
    next_lesson_id: int | None = None


class CourseSummary(BaseModel):
    id: int
    slug: str
    title: str
    domain: str
    description: str
    order: int
    lesson_count: int
    completed_lessons: int = 0
    total_minutes: int


class CourseDetail(CourseSummary):
    lessons: list[LessonSummary]


class QuizSubmission(BaseModel):
    answers: list[int | None] = Field(max_length=100)


class QuestionResultOut(BaseModel):
    question_id: int
    selected_index: int | None
    correct_index: int
    is_correct: bool
    explanation: str


class QuizResultOut(BaseModel):
    score: int
    correct: int
    total: int
    passed: bool
    pass_mark: int
    results: list[QuestionResultOut]
    lesson_status: str
