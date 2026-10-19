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
