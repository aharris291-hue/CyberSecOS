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
| 1 | Networking Foundations for SOC Analysts | Networking | OSI/TCP-IP and the handshake; Ports, services and DNS; Finding malicious traffic in a pcap |
| 2 | Windows Security Monitoring | Windows Security | Security event logs; Sysmon and suspicious PowerShell |
| 3 | Linux Security Operations | Linux Security | SSH brute force in auth.log; Permissions, SUID and hardening |
| 4 | SIEM Fundamentals | SIEM | Searching logs with KQL |
| 5 | Threat Hunting | Threat Hunting | Hypothesis-driven hunting for persistence |
| 6 | Incident Response | Incident Response | IR lifecycle and phishing triage |
| 7 | Detection Engineering | Detection Engineering | Writing and testing Sigma rules |
| 8 | Cloud Security Operations | Cloud Security | Shared responsibility and cloud sign-in investigation |

12 lessons, about 19% theory and 81% lab time overall.

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
