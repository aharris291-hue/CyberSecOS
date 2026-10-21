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
- Tokens are HS256 JWTs, valid for 12 hours by default. Each carries the account's `token_version`; changing the password bumps that version, so every token issued earlier stops authenticating (401).

## Profile

| Method | Path | Description |
|---|---|---|
| GET | `/api/users/me` | Current profile |
| PATCH | `/api/users/me` | Any of `display_name`, `target_role`, `weekly_goal_hours` (1-80) |
| POST | `/api/users/me/password` | `{current_password, new_password}` → a fresh token. Invalidates every token issued before the change (other sessions are signed out); the response token keeps the caller signed in. 400 if the current password is wrong |

## Course engine

| Method | Path | Description |
|---|---|---|
| GET | `/api/certifications` | Certifications in priority order, with objectives |
| GET | `/api/courses` | Courses in order with `lesson_count`, `completed_lessons`, `total_minutes` |
| GET | `/api/courses/{slug}` | Course with its lesson summaries and per-lesson `status` |
| GET | `/api/lessons/{id}` | Full lesson (all six standard sections), quiz questions without answers, mapped certification objectives, the learner's progress and previous/next lesson ids |
| POST | `/api/lessons/{id}/quiz` | `{answers: [index or null, ...]}` in question order → score, `passed`, and per-question `correct_index` and `explanation`. 422 if the answer count is wrong |

## Graded labs

| Method | Path | Description |
|---|---|---|
| GET | `/api/labs` | Every graded lab in course and lesson order, with its lesson and course, task count, pass mark and the learner's `status` (`attempts`, `best_score`, `passed`) |
| GET | `/api/lessons/{id}/lab` | The lesson's graded lab: scenario, pass mark, `artifacts` (name, kind, description, content), `tasks` (index, prompt, `type` of `text`/`number`/`choice`/`set`, options for choices) and the learner's `status`. No answers, hints or explanations. 404 if the lesson has no graded lab |
| GET | `/api/lessons/{id}/lab/artifacts/{name}` | One lab file as a `text/plain` attachment, to work on with grep, awk, jq or a spreadsheet. 404 for an unknown name |
| POST | `/api/lessons/{id}/lab/attempts` | `{answers: [string or null, ...]}` in task order (a choice is the option index as text) → `score`, `correct`, `total`, `passed`, `pass_mark`, per-task `is_correct` and `feedback`, `lab_completed` and `lesson_status`. Feedback is the explanation for a right answer and the hint for a wrong one; once the lab is passed every task gets its explanation, so answers are never handed out on a failed attempt. A first pass completes the lesson's lab and logs its lab minutes once. 422 for the wrong answer count or an answer over 2000 characters |

Answers are normalised before marking: case, surrounding spaces, quotes and a
trailing full stop are ignored; numbers accept thousands separators and an optional
per-task tolerance; set answers are unordered and split on commas, semicolons,
spaces or new lines, and must contain every item and nothing extra.

## Progress tracking

| Method | Path | Description |
|---|---|---|
| POST | `/api/lessons/{id}/theory` | Optional `{minutes}`; marks theory read and logs theory time once |
| POST | `/api/lessons/{id}/lab` | `{notes?, minutes?}` (minutes 1-600, default the lesson's estimate); marks the lab done and logs lab time once. Calling again only updates notes. 409 for a lesson with a graded lab that has not been passed yet (pass it instead); once passed, this still saves notes |
| DELETE | `/api/lessons/{id}/lab` | Reopens a lab marked done by mistake (logged time is kept) |
| GET | `/api/progress` | Totals, this week vs goal, per-course progress, certification readiness, recent activity and the recommended next lesson |
| GET | `/api/progress/activity?days=30` | Daily theory/lab/quiz minutes for the last N days (1-365) |
| GET | `/api/progress/report.pdf` | Downloadable one-page PDF progress report (`application/pdf`, sent as a dated attachment), built from the same figures as `/api/progress` |

`/api/progress` endpoints accept `tz_offset_minutes` (the learner's UTC offset,
-840 to 840) so days, weeks and streaks follow the learner's calendar. The
desktop client sends it automatically.

Definitions:
- **Completed lesson:** lab done and quiz passed (70%+). A graded lab counts as done once passed.
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
| GET | `/target` | The learner's target exam date for this certification, with days remaining, or `null` if none set. Accepts `tz_offset_minutes` |
| PUT | `/target` | Set or update the target date (`{exam_date}`); returns the date and days remaining. Accepts `tz_offset_minutes` |
| DELETE | `/target` | Clear the target date |

- **Objective breakdown:** for each objective the exam touched, correct / total and a percentage, so a result points at the objectives to revise.
- A 404 is returned for an unknown `cert_id`; a 422 if the certification has no questions to build an exam from.
- Certification readiness in `/api/progress` and `/api/dashboard` now includes `target_date` and `days_until_exam` (null when no target is set).

## Study planner

Turns the weekly goal, the target exam dates and the remaining lessons into a
pace check and a schedule. Read-only; derived live, so nothing new is stored.

| Method | Path | Description |
|---|---|---|
| GET | `/api/plan` | The study plan: summary, per-certification pace and a week-by-week schedule. Accepts `tz_offset_minutes` |

- **Summary:** weekly goal (hours and minutes), total remaining lessons and
  minutes, estimated weeks to finish at the current pace and the projected
  finish date, an overall `on_track` flag (null when no exam date is set), and
  the `focus_certification_id` — the soonest exam that still has lessons left.
- **Per-certification pace:** remaining lessons and minutes toward the
  certification's uncovered objectives, days until its exam, the
  `required_minutes_per_week` to finish in time, and a `status` of `ready`
  (all objectives covered), `no_date` (no target set), `on_track`, `behind`,
  or `overdue` (exam date passed with work left).
- **Schedule:** remaining lessons packed into weeks at the weekly goal, with
  soonest-exam work first; each week lists its lessons with their theory and
  lab minutes. A lesson longer than the whole weekly budget takes its own week.

## Advanced analytics

Looks across the learner's history — rather than the current snapshot the
dashboard shows — to surface how study is trending, when they study, how fast
they are really moving and what their quiz scores say to revise. Read-only;
derived live from the same activity, progress and quiz records.

| Method | Path | Description |
|---|---|---|
| GET | `/api/analytics` | Study trend, patterns, learning velocity and quiz weak-spots. Accepts `tz_offset_minutes` and `weeks` (1–52, default 8) |

- **Study trend:** the last `weeks` weeks, each with total minutes, active days
  and a four-week rolling average of minutes, oldest week first.
- **Study patterns:** minutes by weekday (Monday first) and by part of the day
  (Morning / Afternoon / Evening / Night), plus the `peak_part_of_day` (null
  until there is any activity).
- **Learning velocity:** total, completed and remaining lessons, lessons
  completed in the last four weeks, the resulting `lessons_per_week`, and a
  projection of the weeks and date to finish the rest at that actual pace
  (null while the recent pace is zero).
- **Quiz performance:** total attempts, average score, a `score_trend` (the
  later half of attempts minus the earlier half, null under four attempts),
  and the weakest domains and objectives — up to five each, ranked by the
  average best quiz score of the attempted lessons in them.
