# Testing

Definition of Done (`07_DEFINITION_OF_DONE.md`) requires tests for every
feature and a passing build. CI (`.github/workflows/ci.yml`) runs both suites on
every pull request and on pushes to `main`.

## Backend (PyTest, `backend/tests`)

| File | Covers |
|---|---|
| `test_health.py` | App starts, health endpoint |
| `test_database.py` | Tables, JSON columns, unique constraints, cascades |
| `test_auth.py` | First-run registration, single-account rule, password policy, hashing, login, lockout, token tampering and expiry |
| `test_users.py` | Profile read/update and password change, including that a change invalidates older tokens and returns a working fresh one |
| `test_content_standards.py` | Every lesson has the six required sections, valid quizzes, certification mapping, 20/80 split, every focus domain and certification covered |
| `test_seed.py` | Seeding and idempotent re-seeding that keeps progress |
| `test_lab_content.py` | Every course has at least four graded labs; each lab is well formed (scenario, artifacts, at least five tasks, hints that do not give the answer away); every `verify` block derives its answer from the lab data and the key accepts it; lab-specific checks for the answers a regex cannot derive (the Sigma rule, risk scores, the shadowed firewall rule, the encoded PowerShell, hash mismatches, rescan reconciliation, logging coverage gaps, exfiltrated volume, alert noise share, control-mapping gaps, access-review leavers, threat-intel feed hygiene, chain-of-custody gaps, emulation coverage, zero-trust decisions, persistence counts, SOC response metrics, exploit-based patch order, API object ownership, alert correlation and backup gaps against the BIA) |
| `test_server.py` | The packaged server entry point: per-user data folder, exiting when the desktop app's process is gone, and the bundle spec shipping the seed content |
| `test_labs.py` | Answer normalisation for text, number (with tolerance), set and choice; scoring and hint-versus-explanation feedback; lab seeding and idempotent re-seeding; lab detail never exposing the key; artifact download; auth; failed vs passed attempts and how a pass completes the lab and lesson once; graded labs refusing self-marking; validation; the lab list and per-learner status |
| `test_quiz_grading.py` | Scoring, rounding, pass mark, unanswered questions |
| `test_courses_api.py` | Catalog, lesson detail without answers, quiz submission |
| `test_progress.py` | Theory/lab/quiz state machine, summary, streaks, week, time zones, readiness |
| `test_dashboard.py` | Dashboard payload for new and active learners |
| `test_exams.py` | Exam sampling across objectives, 80% grading, per-objective breakdown, submission validation and attempt history |
| `test_exam_targets.py` | Set/get/clear a per-certification target date, countdown maths, and its surfacing in dashboard readiness |
| `test_planner.py` | Study plan: remaining-work schedule packing, weekly-target limits, pace verdicts (on track / behind / overdue / no date), soonest-exam ordering, and the low-goal bound |
| `test_analytics.py` | Advanced analytics: weekly trend bucketing and rolling average, weekday/part-of-day patterns, velocity projection from recent completions, quiz trend and weakest-area ranking, the empty-account payload and the `weeks` bound |
| `test_learner_journey.py` | End-to-end: sign up, study a lesson, see it on the dashboard |

Each test gets a fresh SQLite database in a temp directory. bcrypt uses a low
cost factor in tests only (`CYBERSECOS_BCRYPT_ROUNDS=4`).

## Desktop (xUnit, `frontend/tests/CyberSecOS.Core.Tests`)

View models and the API client are tested without WPF:

- `ApiClientTests`: snake_case JSON, bearer header, error mapping, offline server, malformed responses.
- `LoginViewModelTests`, `ShellTests`, `ProfileViewModelTests`: first-run vs sign-in, validation, navigation, sign-out, expired sessions.
- `CoursesViewModelTests`, `LessonViewModelTests`: catalog, lesson sections, quiz selection, grading and retry.
- `ProgressTrackingTests`: theory/lab commands, checklist, Progress page.
- `DashboardViewModelTests`: tiles, chart data, next lesson, empty state.
- `ExamsViewModelTests`: certification selection, length capping, timer, answering, grading, focus areas, the setup/taking/results flow, and target-date set/clear with countdown.
- `PlannerViewModelTests`: plan load, on-track / behind / complete headlines, per-certification pace phrasing, week summaries, and navigation to a lesson or the exam dates.
- `InsightsViewModelTests`: analytics load into the trend and weekday charts and the weak-area lists, the recent-pace headline, the stalled-learner nudge, the velocity finish projection, the quiz average/trend phrasing, the empty-quiz and peak-time wording, the default eight-week request, and API-error surfacing.
- `GradedLabTests`: the lesson page loading a graded lab with its files and tasks, replacing self-marking with "pass the graded lab", skipping the lab call for ordinary lessons, the answer-everything check, submitting trimmed answers and the choice index, pass and fail feedback with status text, API errors, and the API client's lab endpoints.
- `LabsViewModelTests`: the Labs page grouping labs by course, the passed/in-progress headline, status and detail phrasing, the empty and none-started cases, opening a lab's lesson, API errors, and its place in the side bar.
- `LocalBackendTests`: starting the bundled server only when nothing answers on a local address, never for a remote one or when no server is installed, passing the port, parent process and data folder, moving to a free port when 8000 is taken, reporting a server that dies while starting, and stopping it on exit.
- `MarkdownTextTests`: lesson-theory parsing into paragraphs with bold and inline-code runs, including unmatched markers and wrapped lines.

`Fakes/FakeApi` is an in-memory `ICyberSecApi`; `Fakes/StubHttpHandler` fakes HTTP.

## Converters (xUnit, `frontend/tests/CyberSecOS.Desktop.Tests`)

The WPF value converters depend on `System.Windows` and LiveCharts2 types, so this
project targets `net9.0-windows` and runs on the Windows CI job (the Linux backend job
does not build it). Creating the converter outputs needs no display or UI thread, so the
tests run headless on the CI runner.

- `ConvertersTests`: the pure value converters — visibility from empty/null/bool, the inverse-bool and status-label maps, the Markdown-stripping plain-text converter, enum-name matching, and the exam countdown — plus that each one-way converter's `ConvertBack` throws.
- `ChartConvertersTests`: `ChartSeriesConverter` mapping `ChartData` to stacked-column, row and pie series (including the empty result for non-chart data and unknown parameters, and that pie drops zero-sum series), and `ChartAxisConverter` building category and value axes (labels, hidden separators, a zero floor and the max-value cap, and the empty-data fallback).

`LessonStatusBrushConverter` resolves an `Application` resource, so it is left to the manual
view smoke test below rather than the headless suite.

## Not yet automated

- **XAML view rendering.** Instantiating the views themselves needs an STA WPF visual tree, which the headless CI runner does not host reliably, so it stays a manual smoke test on a Windows desktop: install the build with `CyberSecOS-Setup-<version>.exe` (or `dotnet run` the desktop project against a running API), then sign in and walk the Dashboard, Courses, Labs, a Lesson (including its graded lab), Progress, Study Plan and Practice Exams pages, confirming the charts, status badges and countdowns render. The converter logic those views bind to is covered by `CyberSecOS.Desktop.Tests` above.
- **Installer on a real desktop.** CI builds the installer and smoke-tests the bundled server (`/api/health`), but installing, launching from the Start menu, upgrading and uninstalling are checked by hand on Windows (steps in `docs/15_WINDOWS_QUICKSTART.md`).
