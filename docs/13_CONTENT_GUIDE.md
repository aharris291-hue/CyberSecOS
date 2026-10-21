# Content Guide

Course content lives in `backend/app/seed/` and is loaded into the database at
API startup. Edit the YAML, restart the API, and the change is live; learner
progress is kept because lessons are matched by `slug`.

## Files

- `certifications.yaml`: Security+ (SY0-701), SC-900, SC-200, CySA+ (CS0-003) and their objectives, in the priority order from `09_CERTIFICATION_OBJECTIVES.md`.
- `content/NN_<domain>.yaml`: one course per focus domain, ordered by the file prefix and `order`.
- `labs/NN_<name>.yaml`: one graded lab per file, attached to a lesson by its slug (see [Graded labs](#graded-labs)).

## Current catalog

| # | Course | Domain | Lessons |
|---|---|---|---|
| 1 | Networking Foundations for SOC Analysts | Networking | 7 |
| 2 | Security Foundations | Security Foundations | 7 |
| 3 | Windows Security Monitoring | Windows Security | 7 |
| 4 | Linux Security Operations | Linux Security | 7 |
| 5 | SIEM Fundamentals | SIEM | 7 |
| 6 | Vulnerability Management | Vulnerability Management | 7 |
| 7 | Threat Hunting | Threat Hunting | 7 |
| 8 | Incident Response | Incident Response | 7 |
| 9 | Detection Engineering | Detection Engineering | 7 |
| 10 | Security Architecture | Security Architecture | 7 |
| 11 | Cloud Security Operations | Cloud Security | 7 |
| 12 | Governance, Risk and Compliance | Governance, Risk and Compliance | 7 |

12 courses, 84 lessons, about 18% theory and 82% lab time overall. Every course now has seven lessons.

**Certification coverage:** every objective of all four priority certifications
(Security+ SY0-701, SC-900, SC-200, CySA+ CS0-003) maps to at least one lesson.
`test_content_standards.py` fails the build if any Security+ objective loses its
lesson, or if any priority certification stops being fully covered.

## Lesson schema

Every lesson must include the six sections from `08_EDUCATION_STANDARDS.md`:

```yaml
- slug: unique-kebab-case-id        # never change once published; progress is keyed on it
  title: ...
  order: 1
  difficulty: beginner | intermediate | advanced
  summary: one line shown in the course list
  theory_minutes: 12                # keep theory at or below 25% of the lesson
  lab_minutes: 48
  objectives: ["secplus:4.9", "cysa:1.1"]   # <certification id>:<objective code>
  learning_objectives: [...]        # 1. Learning Objectives
  theory: |                         # 2. Theory (light Markdown: **bold**, `code`)
  examples:                         # 3. Examples
    - {title: ..., body: ...}
  quiz:                             # 4. Quiz (at least 3 questions)
    - prompt: ...
      options: [...]                # at least 3, no duplicates
      answer: 1                     # zero-based index of the correct option
      explanation: ...
  exercise:                         # 5. Practical Exercise (at least 5 steps)
    title: ...
    environment: ...
    tools: [...]
    steps: [...]
    deliverable: ...
  expected_outcome: ...             # 6. Expected Outcome
```

`backend/tests/test_content_standards.py` enforces all of this, plus the 20/80
split across the catalog and coverage of all four priority certifications. A
content change that breaks the standards fails CI.

## Quizzes

The pass mark is 70%. Questions are served without answers; the correct option
and explanation for every question are returned after submission, whether the
learner passed or not, so a failed attempt still teaches.

## Graded labs

A graded lab turns a lesson's practical exercise into work the app can check. The
learner gets a scenario and one or more files to analyse (logs, packet summaries,
CSV exports, JSON events, configs, an email), answers specific questions, and the
server marks the answers. Passing the lab (80% by default) completes the lesson's
lab; it cannot be self-marked.

Labs live in separate files under `backend/app/seed/labs/` so they never collide
with lesson edits. Each lab attaches to exactly one lesson:

```yaml
slug: lab-linux-ssh-bruteforce    # unique; never change once published (attempts are keyed on it)
lesson: linux-auth-logs           # the lesson slug this lab belongs to (one lab per lesson)
title: Prove whether an SSH brute force succeeded
pass_mark: 80                     # optional, default 80
scenario: |                       # the briefing (light Markdown)
  ...
artifacts:                        # at least one; names must be unique within the lab
  - name: auth.log                # also the download file name
    kind: log                     # log | capture | csv | json | config | email | text
    description: ...
    content: |
      ...
tasks:                            # at least five; at least three answered from the data
  - prompt: Which source IP made the most failed password attempts?
    type: text                    # text | number | set | choice
    accepted: [185.220.101.33]    # any of these is right (not used for choice)
    hint: Shown when the answer is wrong. Must not contain the answer.
    explanation: Shown when it is right, or for every task once the lab is passed.
    verify:                       # optional: derive the answer from the data in tests
      artifact: auth.log
      op: top                     # count | distinct_count | set | first | last | top | median_interval
      pattern: 'Failed password for (?:invalid user )?\S+ from (\S+)'
  - prompt: What is the beacon interval in seconds?
    type: number
    accepted: ["300"]
    tolerance: 5                  # number tasks only
  - prompt: Which ports are open?
    type: set                     # unordered; every item and nothing extra
    accepted: ["22, 80, 443"]
  - prompt: What kind of scan is this?
    type: choice
    options: [...]                # at least 3, no duplicates
    answer: 1                     # zero-based index
```

**Keep the answer key honest.** `test_lab_content.py` runs every `verify` block: it
applies the regex line by line to the artifact, derives the answer with the given
operation (group 1 when the regex has a group) and checks the key accepts it. A data
edit that changes an answer therefore fails CI instead of shipping a wrong key. Give
every fact-finding task a `verify` block where a regex can express it; for answers
that need reasoning (a Sigma rule, a risk score, a shadowed firewall rule), add a
lab-specific check in the same test file.

**Keep it safe.** Lab data is analysis-only: no live malware, exploit code or attack
tooling. Use reserved names and addresses (`.example` domains, 192.0.2.0/24,
198.51.100.0/24 and 203.0.113.0/24, AWS's documentation account ID) so nothing in a
lab points at a real system.

### Current labs

| Course | Lesson | Lab |
|---|---|---|
| Networking | `net-traffic-analysis` | Investigate a port scan in a packet capture |
| Security Foundations | `sec-phishing-social-engineering` | Analyse a reported phishing email |
| Windows Security | `win-lateral-movement` | Trace lateral movement through Windows Security logs |
| Linux Security | `linux-auth-logs` | Prove whether an SSH brute force succeeded |
| SIEM | `siem-alert-investigation` | Triage an Office-spawns-PowerShell alert |
| Vulnerability Management | `vm-prioritisation-reporting` | Prioritise a scan export with CVSS, EPSS and KEV |
| Threat Hunting | `hunt-c2-beaconing` | Hunt for command-and-control beaconing |
| Incident Response | `ir-ransomware-response` | Reconstruct a ransomware attack timeline |
| Detection Engineering | `de-sigma-rules` | Predict what a Sigma rule will match |
| Security Architecture | `arch-network-design` | Review a segmentation firewall policy |
| Cloud Security | `cloud-incident-investigation` | Investigate a leaked access key in CloudTrail |
| Governance, Risk and Compliance | `grc-risk-thirdparty-automation` | Score a risk register and assess a vendor |
