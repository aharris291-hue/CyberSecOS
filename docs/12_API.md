# API Reference

Base URL: `http://127.0.0.1:8000`. Interactive docs: `/docs` (Swagger UI).
All endpoints except `/api/health` and `/api/auth/*` require
`Authorization: Bearer <access_token>`. Errors use FastAPI's shape:
`{"detail": "message"}` (or a list of validation errors for 422).

## System

| Method | Path | Description |
|---|---|---|
| GET | `/api/health` | Liveness check, returns version |

## Authentication

CyberSecOS is single-user: the first registration creates the only account.

| Method | Path | Description |
|---|---|---|
| GET | `/api/auth/status` | `{"registered": bool}`; the client shows first-run setup when false |
| POST | `/api/auth/register` | `{username, password, display_name}` → token. 409 once an account exists |
| POST | `/api/auth/login` | `{username, password}` → token. 401 on bad credentials, 423 while locked |

Token response: `{access_token, token_type, expires_in, user}`.

Security rules:
- Passwords: at least 10 characters with a letter and a digit; stored as bcrypt hashes.
- Usernames are case-insensitive (stored lower-case).
- Wrong password and unknown user return the same 401 message.
- 5 consecutive failures lock the account for 5 minutes (`Retry-After` header on 423).
- Tokens are HS256 JWTs, valid for 12 hours by default.

## Profile

| Method | Path | Description |
|---|---|---|
| GET | `/api/users/me` | Current profile |
| PATCH | `/api/users/me` | Any of `display_name`, `target_role`, `weekly_goal_hours` (1-80) |
| POST | `/api/users/me/password` | `{current_password, new_password}` → 204 |

## Course engine

| Method | Path | Description |
|---|---|---|
| GET | `/api/certifications` | Certifications in priority order, with objectives |
| GET | `/api/courses` | Courses in order with `lesson_count`, `completed_lessons`, `total_minutes` |
| GET | `/api/courses/{slug}` | Course with its lesson summaries and per-lesson `status` |
| GET | `/api/lessons/{id}` | Full lesson (all six standard sections), quiz questions without answers, mapped certification objectives, the learner's progress and previous/next lesson ids |
| POST | `/api/lessons/{id}/quiz` | `{answers: [index or null, ...]}` in question order → score, `passed`, and per-question `correct_index` and `explanation`. 422 if the answer count is wrong |

## Progress tracking

| Method | Path | Description |
|---|---|---|
| POST | `/api/lessons/{id}/theory` | Optional `{minutes}`; marks theory read and logs theory time once |
| POST | `/api/lessons/{id}/lab` | `{notes?, minutes?}` (minutes 1-600, default the lesson's estimate); marks the lab done and logs lab time once. Calling again only updates notes |
| DELETE | `/api/lessons/{id}/lab` | Reopens a lab marked done by mistake (logged time is kept) |
| GET | `/api/progress` | Totals, this week vs goal, per-course progress, certification readiness, recent activity and the recommended next lesson |
| GET | `/api/progress/activity?days=30` | Daily theory/lab/quiz minutes for the last N days (1-365) |

`/api/progress` endpoints accept `tz_offset_minutes` (the learner's UTC offset,
-840 to 840) so days, weeks and streaks follow the learner's calendar. The
desktop client sends it automatically.

Definitions:
- **Completed lesson:** lab done and quiz passed (70%+).
- **Streak:** consecutive days with any logged study time; today counts once studied, and the streak survives until the end of today.
- **Week:** Monday 00:00 to now, in the learner's time zone; goal is `weekly_goal_hours` from the profile.
- **Hands-on percent:** lab minutes / all logged minutes; the target is 80%.
- **Certification readiness:** objectives covered by at least one completed lesson / all objectives for that exam.
- **Next lesson:** the first in-progress lesson in course order, otherwise the first not started.

## Dashboard

| Method | Path | Description |
|---|---|---|
| GET | `/api/dashboard?days=14` | One call for the home screen: name and target role, totals, week vs goal, next lesson, daily study minutes for the last `days` (7-90), per-course progress, certification readiness, and the 5 latest activities. Accepts `tz_offset_minutes` |

## Practice exams

Timed, scored mock exams for a certification. Questions are sampled across the
lessons mapped to that certification's objectives, spread round-robin so no one
objective dominates a short exam. Exams are graded at an 80% pass mark (harder
than the 70% lesson quizzes) and return a per-objective breakdown so the learner
can see weak areas. All paths are under `/api/certifications/{cert_id}/exam`.

| Method | Path | Description |
|---|---|---|
| GET | `` | Exam info: certification name and exam code, questions available, default length and the 80% pass mark |
| GET | `/paper?size=20` | A fresh exam paper: `size` questions (5-100, capped at those available) without answers. Each call reshuffles |
| POST | `` | Submit `{answers: [{question_id, selected_index}], duration_seconds}`; grades, records an attempt, and returns score, pass/fail and the per-objective breakdown |
| GET | `/history` | Past attempts for this certification, newest first |

- **Objective breakdown:** for each objective the exam touched, correct / total and a percentage, so a result points at the objectives to revise.
- A 404 is returned for an unknown `cert_id`; a 422 if the certification has no questions to build an exam from.
