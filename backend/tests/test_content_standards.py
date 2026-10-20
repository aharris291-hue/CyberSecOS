"""Bundled lessons must meet docs/08_EDUCATION_STANDARDS.md and map to certifications."""

import pytest

from app.services.seed import SEED_DIR, load_course_files, load_yaml

COURSES = load_course_files()
LESSONS = [(course["slug"], lesson) for course in COURSES for lesson in course["lessons"]]
CERTS = load_yaml(SEED_DIR / "certifications.yaml")
OBJECTIVE_IDS = {f"{cert['id']}:{o['code']}" for cert in CERTS for o in cert["objectives"]}

REQUIRED_DOMAINS = {
    "Networking",
    "Windows Security",
    "Linux Security",
    "SIEM",
    "Threat Hunting",
    "Incident Response",
    "Detection Engineering",
    "Cloud Security",
}


def test_every_focus_domain_has_a_course():
    assert REQUIRED_DOMAINS <= {course["domain"] for course in COURSES}


def test_certifications_follow_priority_order():
    assert [cert["exam_code"] for cert in sorted(CERTS, key=lambda c: c["priority"])] == [
        "SY0-701",
        "SC-900",
        "SC-200",
        "CS0-003",
    ]


def test_slugs_are_unique():
    slugs = [lesson["slug"] for _, lesson in LESSONS]
    assert len(slugs) == len(set(slugs))


@pytest.mark.parametrize(("course", "lesson"), LESSONS, ids=[lesson["slug"] for _, lesson in LESSONS])
def test_lesson_has_every_required_section(course, lesson):
    assert len(lesson["learning_objectives"]) >= 2, "Learning Objectives"
    assert len(lesson["theory"].split()) >= 100, "Theory"
    assert lesson["examples"] and all(e["title"] and e["body"] for e in lesson["examples"]), "Examples"
    assert len(lesson["quiz"]) >= 3, "Quiz"
    exercise = lesson["exercise"]
    assert exercise["environment"] and exercise["deliverable"], "Practical Exercise"
    assert len(exercise["steps"]) >= 5, "Practical Exercise steps"
    assert lesson["expected_outcome"].strip(), "Expected Outcome"


@pytest.mark.parametrize(("course", "lesson"), LESSONS, ids=[lesson["slug"] for _, lesson in LESSONS])
def test_quiz_questions_are_well_formed(course, lesson):
    for question in lesson["quiz"]:
        assert len(question["options"]) >= 3
        assert len(set(map(str, question["options"]))) == len(question["options"]), "duplicate options"
        assert 0 <= question["answer"] < len(question["options"])
        assert question["explanation"].strip()


@pytest.mark.parametrize(("course", "lesson"), LESSONS, ids=[lesson["slug"] for _, lesson in LESSONS])
def test_lesson_maps_to_known_certification_objectives(course, lesson):
    assert lesson["objectives"], "map lessons to certification objectives"
    assert set(lesson["objectives"]) <= OBJECTIVE_IDS


@pytest.mark.parametrize(("course", "lesson"), LESSONS, ids=[lesson["slug"] for _, lesson in LESSONS])
def test_each_lesson_is_mostly_hands_on(course, lesson):
    share = lesson["theory_minutes"] / (lesson["theory_minutes"] + lesson["lab_minutes"])
    assert share <= 0.25, f"theory is {share:.0%} of {lesson['slug']}"


def test_catalog_hits_the_20_80_target():
    theory = sum(lesson["theory_minutes"] for _, lesson in LESSONS)
    lab = sum(lesson["lab_minutes"] for _, lesson in LESSONS)
    assert 0.15 <= theory / (theory + lab) <= 0.22


def test_every_priority_certification_is_covered():
    mapped = {objective.split(":")[0] for _, lesson in LESSONS for objective in lesson["objectives"]}
    assert mapped == {cert["id"] for cert in CERTS}


def _objective_coverage(cert_id: str) -> tuple[set[str], set[str]]:
    cert = next(c for c in CERTS if c["id"] == cert_id)
    all_objectives = {f"{cert_id}:{o['code']}" for o in cert["objectives"]}
    covered = {o for _, lesson in LESSONS for o in lesson["objectives"] if o.startswith(f"{cert_id}:")}
    return all_objectives, covered


def test_every_security_plus_objective_has_a_lesson():
    """Security+ is the first-priority certification, so its coverage must stay complete."""
    all_objectives, covered = _objective_coverage("secplus")
    missing = sorted(code.split(":")[1] for code in all_objectives - covered)
    assert not missing, f"Security+ objectives with no lesson: {missing}"


def test_all_priority_certifications_fully_covered():
    for cert in CERTS:
        all_objectives, covered = _objective_coverage(cert["id"])
        assert all_objectives <= covered, f"{cert['id']} missing {sorted(all_objectives - covered)}"
