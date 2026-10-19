from app.models import CertObjective, Certification, Course, Lesson, LessonProgress, QuizQuestion, User
from app.services.seed import load_course_files, seed_database


def test_startup_seeds_catalog(db):
    files = load_course_files()
    assert db.query(Course).count() == len(files)
    assert db.query(Lesson).count() == sum(len(c["lessons"]) for c in files)
    assert db.query(Certification).count() == 4
    assert db.get(CertObjective, "secplus:4.9").title.startswith("Given a scenario, use data sources")


def test_lessons_link_to_objectives(db):
    lesson = db.query(Lesson).filter_by(slug="win-event-logs").one()
    assert {o.id for o in lesson.objectives} == {"secplus:4.9", "cysa:1.3", "sc200:1"}


def test_reseeding_is_idempotent_and_keeps_progress(db):
    lesson = db.query(Lesson).filter_by(slug="net-osi-tcpip").one()
    user = User(username="adam", password_hash="x", display_name="A")
    db.add(user)
    db.flush()
    db.add(LessonProgress(user_id=user.id, lesson_id=lesson.id, status="in_progress"))
    db.commit()
    counts = (db.query(Course).count(), db.query(Lesson).count(), db.query(QuizQuestion).count())

    seed_database(db)

    assert (db.query(Course).count(), db.query(Lesson).count(), db.query(QuizQuestion).count()) == counts
    assert db.query(Lesson).filter_by(slug="net-osi-tcpip").one().id == lesson.id
    assert db.query(LessonProgress).count() == 1
