"""Bundled graded labs must be well formed, and their answer keys must agree with their data.

Most fact-finding tasks carry a ``verify`` block: a regex run line by line over one artifact
plus an operation that derives the answer the same way a learner would (count, most common
value, distinct values...). The test derives it and checks the answer key accepts it, so an
edit to the data that changes the answer cannot ship with a stale key. Tasks that need
reasoning a regex cannot express are checked by the lab-specific tests at the end.
"""

from __future__ import annotations

import json
import re
import statistics
from collections import Counter

import pytest

from app.services.lab import TASK_TYPES, is_correct
from app.services.seed import SEED_DIR, lab_tasks, load_course_files, load_lab_files

LABS = load_lab_files()
LESSONS = {lesson["slug"]: (course, lesson) for course in load_course_files() for lesson in course["lessons"]}
IDS = [lab["slug"] for lab in LABS]
# Labs whose answers are derived by a dedicated test below instead of (or as well as) verify blocks.
CHECKED_BY_CODE = {"lab-de-sigma-matching", "lab-grc-risk-register", "lab-arch-firewall-review", "lab-siem-alert-triage"}


def _artifact(lab: dict, name: str) -> str:
    return next(a["content"] for a in lab["artifacts"] if a["name"] == name)


def _task(lab: dict, prompt_start: str) -> dict:
    return next(t for t in lab_tasks(lab) if t["prompt"].startswith(prompt_start))


def derive(content: str, spec: dict) -> str:
    pattern = re.compile(spec["pattern"])
    values = []
    for line in content.splitlines():
        match = pattern.search(line)
        if match:
            values.append(match.group(1) if pattern.groups else match.group(0))
    assert values, f"verify pattern matched nothing: {spec['pattern']}"
    op = spec["op"]
    if op == "count":
        return str(len(values))
    if op == "distinct_count":
        return str(len(set(values)))
    if op == "set":
        return ", ".join(sorted(set(values)))
    if op == "first":
        return values[0]
    if op == "last":
        return values[-1]
    if op == "top":
        (top, n), *rest = Counter(values).most_common()
        assert not rest or rest[0][1] < n, "verify 'top' is a tie"
        return top
    if op == "median_interval":
        stamps = sorted(int(v) for v in values)
        return str(statistics.median(b - a for a, b in zip(stamps, stamps[1:])))
    raise AssertionError(f"unknown verify op {op}")


def test_every_course_has_a_graded_lab():
    courses_with_labs = {LESSONS[lab["lesson"]][0]["slug"] for lab in LABS}
    assert courses_with_labs == {course["slug"] for course in load_course_files()}


def test_lab_slugs_and_lessons_are_unique():
    assert len(IDS) == len(set(IDS))
    lessons = [lab["lesson"] for lab in LABS]
    assert len(lessons) == len(set(lessons))


@pytest.mark.parametrize("lab", LABS, ids=IDS)
def test_lab_is_well_formed(lab):
    assert lab["lesson"] in LESSONS, "lab must attach to a bundled lesson"
    assert len(lab["scenario"].split()) >= 40, "scenario must brief the learner"
    assert lab["artifacts"] and all(a["content"].strip() and a["description"] for a in lab["artifacts"])
    tasks = lab_tasks(lab)  # raises ContentError on a malformed task
    assert len(tasks) >= 5, "a graded lab needs at least five tasks"
    assert {t["type"] for t in tasks} <= set(TASK_TYPES)
    assert sum(t["type"] != "choice" for t in tasks) >= 3, "most tasks must be answered from the data, not picked"
    for task in tasks:
        if task["type"] == "choice":
            assert len(task["options"]) >= 3 and len(set(task["options"])) == len(task["options"])
        # The hint must not just be the answer.
        assert all(a.lower() not in task["hint"].lower() for a in task["accepted"] if len(a) > 3)


@pytest.mark.parametrize("lab", LABS, ids=IDS)
def test_answer_keys_match_the_lab_data(lab):
    verified = 0
    for raw, task in zip(lab["tasks"], lab_tasks(lab), strict=True):
        if "verify" in raw:
            derived = derive(_artifact(lab, raw["verify"]["artifact"]), raw["verify"])
            assert is_correct(task, derived), f"{task['prompt']!r}: data gives {derived!r}, key says {task['accepted']}"
            verified += 1
    assert verified >= 1 or lab["slug"] in CHECKED_BY_CODE, "check at least one answer against the data"


def test_lab_files_live_next_to_the_content():
    assert (SEED_DIR / "labs").is_dir()


# --- Tasks whose answers need more than a regex --------------------------------


def _lab(slug: str) -> dict:
    return next(lab for lab in LABS if lab["slug"] == slug)


def test_sigma_lab_answers_follow_the_rule():
    lab = _lab("lab-de-sigma-matching")
    events = [json.loads(line) for line in _artifact(lab, "process_events.jsonl").splitlines()]

    def shape(e):
        cli = e["CommandLine"].lower()
        return e["Image"].lower().endswith("\\certutil.exe") and ("-urlcache" in cli or "/urlcache" in cli) and "http" in cli

    def filtered(e):
        return e["ParentImage"].lower().endswith("\\inventoryagent.exe")

    matches = ", ".join(str(e["RecordID"]) for e in events if shape(e) and not filtered(e))
    assert is_correct(_task(lab, "Which RecordIDs"), matches)
    assert is_correct(_task(lab, "How many events would match"), str(sum(shape(e) for e in events)))
    renamed = [e for e in events if "-urlcache" in e["CommandLine"] and not e["Image"].lower().endswith("certutil.exe")]
    assert len(renamed) == 1 and is_correct(_task(lab, "Which event is a real download"), str(renamed[0]["RecordID"]))


def test_risk_register_lab_scores_follow_the_method():
    lab = _lab("lab-grc-risk-register")
    rows = [line.split(",") for line in _artifact(lab, "risk_register.csv").splitlines()[1:]]
    scores = {row[0]: int(row[2]) * int(row[3]) for row in rows}
    top = max(scores, key=scores.get)
    assert list(scores.values()).count(scores[top]) == 1
    assert is_correct(_task(lab, "Which risk ID has the highest"), top)
    assert is_correct(_task(lab, "Which risk IDs are above appetite"), ", ".join(k for k, v in scores.items() if v >= 15))
    assert is_correct(_task(lab, "What is the score of R-04"), str(scores["R-04"]))


def test_firewall_lab_shadowed_rule_is_really_shadowed():
    lab = _lab("lab-arch-firewall-review")
    rules = {}
    for line in _artifact(lab, "firewall_rules.txt").splitlines():
        parts = line.split()
        if parts and parts[0].isdigit():
            rules[int(parts[0])] = parts[1:5]
    shadowed = [
        rid
        for rid, (_, src, dst, svc) in rules.items()
        if any(
            earlier < rid and e_src in (src, "any") and e_dst in (dst, "any") and e_svc in (svc, "any")
            for earlier, (_, e_src, e_dst, e_svc) in rules.items()
        )
    ]
    assert shadowed == [10]
    assert is_correct(_task(lab, "Which rule ID can never take effect"), "10")


def test_siem_lab_encoded_command_decodes_to_the_answer():
    import base64

    lab = _lab("lab-siem-alert-triage")
    encoded = re.search(r"-enc (\S+?)\"", _artifact(lab, "endpoint_events.jsonl")).group(1)
    decoded = base64.b64decode(encoded).decode("utf-16-le")
    domain = re.search(r"https?://([^/]+)/", decoded).group(1)
    assert is_correct(_task(lab, "Decode the PowerShell"), domain)
