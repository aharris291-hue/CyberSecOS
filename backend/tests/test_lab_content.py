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
CHECKED_BY_CODE = {
    "lab-de-sigma-matching",
    "lab-grc-risk-register",
    "lab-arch-firewall-review",
    "lab-siem-alert-triage",
    "lab-sec-hashes-certificates",
    "lab-vm-remediation-verification",
    "lab-arch-logging-coverage",
    "lab-hunt-data-exfiltration",
    "lab-de-tuning-noisy-rule",
    "lab-grc-control-mapping",
}


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


def test_every_course_has_at_least_four_graded_labs():
    per_course = Counter(LESSONS[lab["lesson"]][0]["slug"] for lab in LABS)
    assert min(per_course.values()) >= 4, per_course


def _csv(lab: dict, name: str, sep: str = ",") -> list[dict]:
    header, *rows = _artifact(lab, name).splitlines()
    keys = header.split(sep)
    return [dict(zip(keys, row.split(sep), strict=True)) for row in rows]


def test_hash_lab_mismatch_is_the_only_one():
    lab = _lab("lab-sec-hashes-certificates")

    def hashes(name):
        return {parts[1]: parts[0] for parts in (line.split() for line in _artifact(lab, name).splitlines()) if len(parts) == 2 and len(parts[0]) == 64}

    published, computed = hashes("SHA256SUMS"), hashes("computed_hashes.txt")
    assert published.keys() == computed.keys()
    mismatched = [name for name in published if published[name] != computed[name]]
    assert len(mismatched) == 1 and is_correct(_task(lab, "Which downloaded file"), mismatched[0])


def test_rescan_lab_reconciles_scans_and_tickets():
    lab = _lab("lab-vm-remediation-verification")
    before = {(r["host"], r["cve"]) for r in _csv(lab, "scan_before.csv")}
    after = {(r["host"], r["cve"]) for r in _csv(lab, "scan_after.csv")}
    assert is_correct(_task(lab, "How many of last week's findings are verified fixed"), str(len(before - after)))
    new = after - before
    assert len(new) == 1 and is_correct(_task(lab, "Which CVE is new"), next(iter(new))[1])
    still_open_but_resolved = [
        parts[0]
        for parts in (line.split() for line in _artifact(lab, "tickets.txt").splitlines())
        if parts and parts[0].startswith("CHG-") and parts[-1] == "Resolved" and (parts[1], parts[2]) in after
    ]
    assert is_correct(_task(lab, "Which findings did an owner mark Resolved"), ", ".join(still_open_but_resolved))


def test_logging_lab_coverage_gaps_follow_the_data():
    from datetime import datetime, timedelta

    lab = _lab("lab-arch-logging-coverage")
    assets = {r["asset"]: r["criticality"] for r in _csv(lab, "asset_inventory.csv")}
    report = datetime.fromisoformat("2026-10-21T00:00:00+00:00")
    last = {r["source"]: datetime.fromisoformat(r["last_event"].replace("Z", "+00:00")) for r in _csv(lab, "siem_sources.csv")}
    missing = [a for a in assets if a not in last]
    silent = [s for s, t in last.items() if report - t > timedelta(hours=24)]
    assert is_correct(_task(lab, "Which assets are not onboarded"), ", ".join(missing))
    assert is_correct(_task(lab, "Which onboarded sources have been silent"), ", ".join(silent))
    critical = [a for a in missing + silent if assets[a] == "critical"]
    assert is_correct(_task(lab, "How many critical assets"), str(len(critical)))


def test_exfiltration_lab_total_upload_matches():
    lab = _lab("lab-hunt-data-exfiltration")
    rows = [r for r in _csv(lab, "proxy.csv") if r["category"] == "uncategorized"]
    total_mb = sum(int(r["bytes_out"]) for r in rows) / 1_000_000
    assert is_correct(_task(lab, "About how many megabytes"), str(round(total_mb)))


def test_tuning_lab_noise_share_matches():
    lab = _lab("lab-de-tuning-noisy-rule")
    rows = _csv(lab, "alerts.txt", sep="|")
    share = 100 * sum(r["parent_image"].endswith("\\CcmExec.exe") for r in rows) / len(rows)
    assert is_correct(_task(lab, "What percentage of all alerts"), str(round(share)))


def test_mapping_lab_gaps_and_blast_radius_follow_the_data():
    lab = _lab("lab-grc-control-mapping")
    required = [line.split()[0] for line in _artifact(lab, "target_profile.txt").splitlines() if re.match(r"^[A-Z]{2}\.[A-Z]{2}-\d{2}\s", line)]
    controls = {r["id"]: [m.strip() for m in r["maps_to"].split(";")] for r in _csv(lab, "control_library.csv", sep="|")}
    mapped = {m for maps in controls.values() for m in maps}
    gaps = [r for r in required if r not in mapped]
    assert len(required) == 10
    assert is_correct(_task(lab, "Which required CSF subcategories have no mapped control"), ", ".join(gaps))
    assert is_correct(_task(lab, "How many of the ten required"), str(len(required) - len(gaps)))
    affected = [m for m in controls["C-06"] if m in required]
    assert is_correct(_task(lab, "Control C-06 is tested"), ", ".join(affected))


def test_access_review_leaver_and_admin_findings_follow_the_data():
    lab = _lab("lab-sec-access-review")
    accounts = {r["username"]: r for r in _csv(lab, "directory_export.csv")}
    leavers = [line.split()[0] for line in _artifact(lab, "hr_leavers.txt").splitlines() if re.match(r"^[a-z]\.\w+\s+\d{4}-", line)]
    active_leavers = [u for u in leavers if accounts.get(u, {}).get("status") == "active"]
    assert len(leavers) == 3 and is_correct(_task(lab, "Which leaver still has"), ", ".join(active_leavers))


def test_ti_lab_feed_hygiene_follows_the_feed():
    lab = _lab("lab-siem-ti-matching")
    feed = _csv(lab, "ti_feed.csv")
    errors = [r["indicator"] for r in feed if r["context"].startswith("Feed error")]
    assert errors and is_correct(_task(lab, "Which feed indicator is a feed error"), errors[0])
    live_high = {r["indicator"] for r in feed if r["confidence"] == "high" and r["expires"] > "2026-10-14"}
    hosts = {r["host"] for r in _csv(lab, "connections.csv") if r["dest_ip"] in live_high or r["dest_domain"] in live_high}
    assert is_correct(_task(lab, "Which hosts matched a high-confidence"), ", ".join(hosts))


def test_custody_lab_hash_mismatch_and_gap_follow_the_log():
    lab = _lab("lab-ir-chain-of-custody")
    rows = [line.split() for line in _artifact(lab, "chain_of_custody.txt").splitlines() if line.startswith("EV-")]
    recorded = {r[0]: r[-1] for r in rows if len(r[-1]) == 16}
    today = {m.group(2): m.group(1)[:16] for m in re.finditer(r"^([0-9a-f]{64})\s+/evidence/(EV-\d+)_", _artifact(lab, "verification.txt"), re.M)}
    changed = [ev for ev, h in today.items() if recorded[ev] != h]
    assert is_correct(_task(lab, "Which evidence item's hash"), ", ".join(changed))
    holder, gaps = {}, []
    for line in _artifact(lab, "chain_of_custody.txt").splitlines():
        m = re.search(r"^(EV-\d+) .*\s(?:Transferred|Checked out for analysis|Returned)\s+(\S+)\s+(\S+)\s+\w{16}$", line)
        if m:
            ev, src, dst = m.groups()
            if holder.get(ev, src) != src:
                gaps.append(src)
            holder[ev] = dst
        elif (m := re.search(r"^(EV-\d+) .*\s-\s+(\S+)\s+\w{16}$", line)):
            holder[m.group(1)] = m.group(2)
    assert gaps and is_correct(_task(lab, "Which person returned"), ", ".join(gaps))


def test_coverage_lab_gaps_follow_the_library_and_results():
    lab = _lab("lab-de-attack-coverage")
    library = {r["technique"]: r["rule"] for r in _csv(lab, "detection_library.csv", sep="|")}
    results = {r["technique"]: r["result"] for r in _csv(lab, "emulation_results.csv", sep="|")}
    missed = [t for t, res in results.items() if res == "no alert"]
    failing = [t for t in missed if t in library]
    assert is_correct(_task(lab, "Which technique has a rule"), ", ".join(failing))
    assert is_correct(_task(lab, "Which emulated techniques have no rule"), ", ".join(t for t in missed if t not in library))
    untested = [rule for t, rule in library.items() if t not in results]
    assert is_correct(_task(lab, "Which rule in the library was not exercised"), ", ".join(untested))


def test_zero_trust_lab_decisions_follow_the_policy():
    lab = _lab("lab-arch-zero-trust-policy")
    rows = _csv(lab, "access_requests.csv", sep="|")

    def allowed(r):
        return (
            r["group"] == "Finance"
            and r["device"] == "managed"
            and r["device_state"] == "compliant"
            and r["mfa_method"] in {"fido2", "hello"}
            and r["signin_risk"] != "high"
        )

    assert is_correct(_task(lab, "Which requests would the policy allow"), ", ".join(r["request"] for r in rows if allowed(r)))
    blocked_office = [r for r in rows if r["network"] == "office" and not allowed(r)]
    assert is_correct(_task(lab, "How many requests come from the office"), str(len(blocked_office)))


def test_persistence_lab_counts_each_mechanism_once():
    lab = _lab("lab-linux-persistence-hunt")
    keys = {m.group(1) for m in re.finditer(r'key="(\w+)"', _artifact(lab, "audit.log"))}
    assert is_correct(_task(lab, "How many separate persistence"), str(len(keys)))


def test_metrics_lab_numbers_follow_the_tickets():
    from datetime import datetime

    lab = _lab("lab-siem-soc-metrics")
    rows = _csv(lab, "incidents.csv")

    def minutes(row, end):
        return (datetime.fromisoformat(row[end].replace("Z", "+00:00")) - datetime.fromisoformat(row["created"].replace("Z", "+00:00"))).total_seconds() / 60

    ack = [minutes(r, "acknowledged") for r in rows]
    assert is_correct(_task(lab, "What was the median time to acknowledge"), str(round(statistics.median(ack))))
    urgent = [r for r in rows if r["severity"] in {"high", "critical"}]
    breaches = [r["incident"] for r in urgent if minutes(r, "acknowledged") > 15]
    assert is_correct(_task(lab, "Which high or critical incidents breached"), ", ".join(breaches))
    assert is_correct(_task(lab, "What share of high and critical"), str(round(100 * (len(urgent) - len(breaches)) / len(urgent))))
    slowest = max(rows, key=lambda r: minutes(r, "resolved"))["incident"]
    assert is_correct(_task(lab, "Which incident took longest"), slowest)


def test_prioritisation_lab_order_follows_the_standard():
    lab = _lab("lab-vm-exploit-prioritisation")
    rows = _csv(lab, "findings.csv")
    ordered = sorted(
        rows,
        key=lambda r: (r["known_exploited"] != "yes" or r["exposure"] != "internet", r["known_exploited"] != "yes", -float(r["epss"]), -float(r["cvss"])),
    )
    assert is_correct(_task(lab, "Which finding is the third priority"), ordered[2]["cve"])


def test_api_lab_foreign_invoices_follow_the_owner_table():
    lab = _lab("lab-arch-api-authorization")
    owners = {r["invoice"]: r["owner"] for r in _csv(lab, "invoice_owners.csv")}
    requests = [json.loads(line) for line in _artifact(lab, "api_gateway.log").splitlines()]
    read = {r["path"].rsplit("/", 1)[1] for r in requests if r["token_sub"] == "cust-1043" and r["status"] == 200}
    foreign = [i for i in read if owners[i] != "cust-1043"]
    assert is_correct(_task(lab, "How many of those invoices belonged"), str(len(foreign)))
    others = [r for r in requests if r["token_sub"] != "cust-1043"]
    assert all(owners[r["path"].rsplit("/", 1)[1]] == r["token_sub"] for r in others), "only the attacker reads foreign invoices"


def test_correlation_lab_incident_follows_the_correlation_key():
    lab = _lab("lab-de-alert-correlation")
    alerts = [r for r in _csv(lab, "alerts.csv", sep="|") if r["user"] == "m.ruiz"]
    assets = {r["host"]: r["criticality"] for r in _csv(lab, "assets.csv", sep="|")}
    hosts = {r["host"] for r in alerts}
    assert is_correct(_task(lab, "How many distinct hosts"), str(len(hosts)))
    assert is_correct(_task(lab, "Which host in the incident is critical"), ", ".join(h for h in hosts if assets[h] == "critical"))


def test_bia_lab_gaps_follow_the_targets():
    lab = _lab("lab-grc-bia-backup-gaps")
    rpo = {r["service"]: float(r["rpo_hours"]) for r in _csv(lab, "bia.csv")}
    tier = {r["service"]: r["tier"] for r in _csv(lab, "bia.csv")}
    backups = {r["service"]: r for r in _csv(lab, "backups.csv", sep="|")}
    misses = [s for s, b in backups.items() if float(b["interval_hours"]) > rpo[s]]
    assert is_correct(_task(lab, "Which services' backups run less often"), ", ".join(misses))
    proven = [s for s, b in backups.items() if b["result"] == "pass" and b["last_restore_test"] >= "2026-04-21"]
    assert is_correct(_task(lab, "How many services have a backup that is proven"), str(len(proven)))
    worst = [s for s in misses if tier[s] == "critical" and s not in proven]
    assert is_correct(_task(lab, "Which critical service is the biggest"), ", ".join(worst))
