"""Pydantic request and response models."""

from __future__ import annotations

from datetime import date, datetime

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


# --- Progress --------------------------------------------------------------


class TheoryRequest(BaseModel):
    minutes: int | None = Field(default=None, ge=1, le=600)


class LabRequest(BaseModel):
    notes: str | None = Field(default=None, max_length=20000)
    minutes: int | None = Field(default=None, ge=1, le=600)


class ProgressTotals(BaseModel):
    total_lessons: int
    completed_lessons: int
    in_progress_lessons: int
    completion_percent: int
    labs_completed: int
    quizzes_passed: int
    quiz_attempts: int
    average_quiz_score: int | None
    theory_minutes: int
    lab_minutes: int
    quiz_minutes: int
    total_minutes: int
    hands_on_percent: int | None
    current_streak_days: int
    longest_streak_days: int


class CourseProgress(BaseModel):
    slug: str
    title: str
    domain: str
    total_lessons: int
    completed_lessons: int
    percent: int


class CertificationProgress(BaseModel):
    id: str
    name: str
    exam_code: str
    objectives_total: int
    objectives_with_content: int
    objectives_mastered: int
    readiness_percent: int
    target_date: date | None = None
    days_until_exam: int | None = None


class ActivityOut(BaseModel):
    kind: str
    minutes: int
    lesson_id: int | None
    lesson_title: str | None
    created_at: datetime


class DailyMinutes(BaseModel):
    date: str
    theory_minutes: int
    lab_minutes: int
    quiz_minutes: int
    total_minutes: int


class WeekProgress(BaseModel):
    week_start: str
    minutes: int
    goal_minutes: int
    goal_percent: int


class NextLesson(BaseModel):
    id: int
    title: str
    course_title: str
    status: str
    theory_minutes: int
    lab_minutes: int


class ProgressSummary(BaseModel):
    totals: ProgressTotals
    week: WeekProgress
    courses: list[CourseProgress]
    certifications: list[CertificationProgress]
    recent_activity: list[ActivityOut]
    next_lesson: NextLesson | None


# --- Dashboard ---------------------------------------------------------------


class Dashboard(BaseModel):
    display_name: str
    target_role: str
    totals: ProgressTotals
    week: WeekProgress
    next_lesson: NextLesson | None
    daily_activity: list[DailyMinutes]
    courses: list[CourseProgress]
    certifications: list[CertificationProgress]
    recent_activity: list[ActivityOut]


# --- Practice exams --------------------------------------------------------


class ExamInfo(BaseModel):
    """What the client needs to offer an exam for a certification."""

    certification_id: str
    name: str
    exam_code: str
    available_questions: int
    default_size: int
    pass_mark: int


class ExamPaper(BaseModel):
    certification_id: str
    name: str
    exam_code: str
    pass_mark: int
    questions: list[QuestionOut]


class ExamAnswer(BaseModel):
    question_id: int
    selected_index: int | None = None


class ExamSubmission(BaseModel):
    answers: list[ExamAnswer] = Field(min_length=1, max_length=200)
    duration_seconds: int | None = Field(default=None, ge=0, le=86400)


class ObjectiveScore(BaseModel):
    code: str
    title: str
    correct: int
    total: int
    percent: int


class ExamResultOut(BaseModel):
    certification_id: str
    score: int
    correct: int
    total: int
    passed: bool
    pass_mark: int
    breakdown: list[ObjectiveScore]


class ExamAttemptOut(BaseModel):
    id: int
    certification_id: str
    score: int
    correct: int
    total: int
    passed: bool
    duration_seconds: int | None
    created_at: datetime

class ExamTargetUpdate(BaseModel):
    exam_date: date


class ExamTargetOut(BaseModel):
    certification_id: str
    exam_date: date
    days_until_exam: int


# --- Study planner ---------------------------------------------------------


class CertificationPace(BaseModel):
    id: str
    name: str
    exam_code: str
    target_date: date | None = None
    days_until_exam: int | None = None
    remaining_lessons: int
    remaining_minutes: int
    required_minutes_per_week: int | None = None
    status: str  # ready | no_date | overdue | on_track | behind


class PlannedLesson(BaseModel):
    id: int
    title: str
    course_title: str
    theory_minutes: int
    lab_minutes: int
    target_date: date | None = None


class PlanWeek(BaseModel):
    week_index: int
    week_start: str
    planned_minutes: int
    lessons: list[PlannedLesson]


class StudyPlan(BaseModel):
    weekly_goal_hours: int
    weekly_target_minutes: int
    total_remaining_lessons: int
    total_remaining_minutes: int
    estimated_weeks_to_finish: int
    estimated_finish_date: date | None = None
    on_track: bool | None = None
    focus_certification_id: str | None = None
    certifications: list[CertificationPace]
    schedule: list[PlanWeek]


# --- Advanced analytics ----------------------------------------------------


class WeeklyStudy(BaseModel):
    week_start: str
    minutes: int
    active_days: int
    rolling_average_minutes: int


class WeekdayStudy(BaseModel):
    weekday: int  # 0 = Monday
    label: str
    minutes: int


class PartOfDayStudy(BaseModel):
    part: str  # Morning | Afternoon | Evening | Night
    minutes: int


class StudyPatterns(BaseModel):
    by_weekday: list[WeekdayStudy]
    by_part_of_day: list[PartOfDayStudy]
    peak_part_of_day: str | None = None


class LearningVelocity(BaseModel):
    total_lessons: int
    completed_lessons: int
    remaining_lessons: int
    completed_last_4_weeks: int
    lessons_per_week: float
    projected_weeks_to_finish: int | None = None
    projected_finish_date: date | None = None


class WeakDomain(BaseModel):
    domain: str
    average_score: int
    lessons_attempted: int


class WeakObjective(BaseModel):
    id: str
    title: str
    certification: str
    average_score: int
    lessons_attempted: int


class QuizPerformance(BaseModel):
    total_attempts: int
    average_score: int | None = None
    score_trend: int | None = None
    weakest_domains: list[WeakDomain]
    weakest_objectives: list[WeakObjective]


class Analytics(BaseModel):
    study_trend: list[WeeklyStudy]
    study_patterns: StudyPatterns
    velocity: LearningVelocity
    quiz_performance: QuizPerformance

