import pytest
from sqlalchemy import inspect
from sqlalchemy.exc import IntegrityError

from app.models import Course, Lesson, LessonProgress, QuizQuestion, User

EXPECTED_TABLES = {
    "users",
    "certifications",
    "cert_objectives",
    "courses",
    "lessons",
    "lesson_objectives",
    "quiz_questions",
    "lesson_progress",
    "quiz_attempts",
    "activity_log",
}


def _lesson(course: Course, slug: str = "l1") -> Lesson:
    return Lesson(
        slug=slug,
        course=course,
        title="Lesson",
        order=1,
        summary="s",
        learning_objectives=["a"],
        theory="t",
        examples=[{"title": "e", "body": "b"}],
        exercise={"title": "x", "steps": ["1"]},
        expected_outcome="o",
        theory_minutes=10,
        lab_minutes=40,
    )


def test_startup_creates_all_tables(client):
    tables = set(inspect(client.app.state.engine).get_table_names())
    assert EXPECTED_TABLES <= tables


def test_lesson_round_trips_json_fields(db):
    course = Course(slug="c", title="C", domain="Networking", description="d", order=1)
    lesson = _lesson(course)
    lesson.questions.append(QuizQuestion(order=1, prompt="p", options=["a", "b"], correct_index=1, explanation="e"))
    db.add(course)
    db.commit()
    db.expire_all()

    stored = db.get(Lesson, lesson.id)
    assert stored.examples == [{"title": "e", "body": "b"}]
    assert stored.exercise["steps"] == ["1"]
    assert stored.questions[0].options == ["a", "b"]


def test_usernames_are_unique(db):
    db.add_all([User(username="adam", password_hash="x", display_name="A"),
                User(username="adam", password_hash="y", display_name="B")])
    with pytest.raises(IntegrityError):
        db.commit()


def test_one_progress_row_per_user_and_lesson(db):
    course = Course(slug="c", title="C", domain="Networking", description="d", order=1)
    lesson = _lesson(course)
    user = User(username="adam", password_hash="x", display_name="A")
    db.add_all([course, user])
    db.commit()
    db.add_all([LessonProgress(user_id=user.id, lesson_id=lesson.id),
                LessonProgress(user_id=user.id, lesson_id=lesson.id)])
    with pytest.raises(IntegrityError):
        db.commit()


def test_deleting_course_cascades_to_lessons_and_questions(db):
    course = Course(slug="c", title="C", domain="Networking", description="d", order=1)
    lesson = _lesson(course)
    lesson.questions.append(QuizQuestion(order=1, prompt="p", options=["a", "b"], correct_index=0, explanation="e"))
    db.add(course)
    db.commit()
    db.delete(course)
    db.commit()
    assert db.query(Lesson).count() == 0
    assert db.query(QuizQuestion).count() == 0
