# Data Model

SQLite database managed by SQLAlchemy 2.0 (`backend/app/models.py`). Tables are
created at API startup; all timestamps are stored as naive UTC.

## Content (seeded, read-only)

| Table | Purpose |
|---|---|
| `certifications` | Security+, SC-900, SC-200, CySA+ with their priority order |
| `cert_objectives` | Exam objectives, id format `<cert>:<code>` (e.g. `secplus:4.9`) |
| `courses` | One course per focus domain (Networking, Windows Security, ...) |
| `lessons` | Learning objectives, theory, examples, practical exercise, expected outcome, theory/lab minutes |
| `lesson_objectives` | Many-to-many map from lessons to certification objectives |
| `quiz_questions` | Multiple-choice questions with the correct option and an explanation |
| `labs` | A graded lab attached to one lesson (unique `lesson_id`): scenario, pass mark, `artifacts` (JSON: name, kind, description, content) and `tasks` (JSON: prompt, type, options, accepted answers, tolerance, hint, explanation). The answer key never leaves the server |

## Learner state

| Table | Purpose |
|---|---|
| `users` | The single learner account: credentials (bcrypt hash), profile, lockout counters, and a `token_version` bumped on password change to invalidate old tokens |
| `lesson_progress` | One row per user and lesson: theory read, lab done, best quiz score, completion |
| `quiz_attempts` | Every quiz submission with score and chosen answers |
| `lab_attempts` | Every graded-lab submission: score, correct/total, pass/fail and the answers as typed. Attempts, best score and pass state per lab are derived from these rows |
| `activity_log` | Study minutes by kind (`theory`, `lab`, `quiz`) for streaks and weekly hours |
| `exam_attempts` | Each practice-exam sitting: certification, score, pass/fail, duration and per-objective breakdown (JSON) |
| `exam_targets` | A learner's target sitting date per certification (one row per user and certification) |

## Lesson completion rule

A lesson is **completed** when its practical exercise is marked done **and** its
quiz has been passed (score of at least 70%). For a lesson with a graded lab the
exercise is marked done only by **passing the lab** (default 80%); it cannot be
self-marked, so completion shows the learner could actually do the analysis. Reading the theory alone leaves it
**in progress**. This keeps hands-on work at the centre of progress, per the
80% hands-on target.
