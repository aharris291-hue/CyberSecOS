# Content Guide

Course content lives in `backend/app/seed/` and is loaded into the database at
API startup. Edit the YAML, restart the API, and the change is live; learner
progress is kept because lessons are matched by `slug`.

## Files

- `certifications.yaml`: Security+ (SY0-701), SC-900, SC-200, CySA+ (CS0-003) and their objectives, in the priority order from `09_CERTIFICATION_OBJECTIVES.md`.
- `content/NN_<domain>.yaml`: one course per focus domain, ordered by the file prefix and `order`.

## Current catalog

| # | Course | Domain | Lessons |
|---|---|---|---|
| 1 | Networking Foundations for SOC Analysts | Networking | 4 |
| 2 | Security Foundations | Security Foundations | 4 |
| 3 | Windows Security Monitoring | Windows Security | 4 |
| 4 | Linux Security Operations | Linux Security | 4 |
| 5 | SIEM Fundamentals | SIEM | 5 |
| 6 | Vulnerability Management | Vulnerability Management | 4 |
| 7 | Threat Hunting | Threat Hunting | 5 |
| 8 | Incident Response | Incident Response | 5 |
| 9 | Detection Engineering | Detection Engineering | 5 |
| 10 | Security Architecture | Security Architecture | 4 |
| 11 | Cloud Security Operations | Cloud Security | 4 |
| 12 | Governance, Risk and Compliance | Governance, Risk and Compliance | 4 |

12 courses, 52 lessons, about 18% theory and 82% lab time overall. Every course has at least four lessons; the daily-driver SOC courses (SIEM, Threat Hunting, Incident Response, Detection Engineering) have five.

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
