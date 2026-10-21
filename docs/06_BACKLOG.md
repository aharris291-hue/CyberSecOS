# Product Backlog

High Priority
- User Profiles
- Dashboard
- Learning Engine
- Quiz Engine
- Progress Tracking

Medium Priority
- AI Mentor
- Study Planner
- Certification Tracking

Low Priority
- Cyber Range
- Advanced Analytics

## Status

| Item | Priority | Status |
|---|---|---|
| User Profiles | High | Done: single-user account, profile and password change |
| Dashboard | High | Done: stats, next lesson, weekly goal, LiveCharts2 charts |
| Learning Engine | High | Done: 12 courses, 21 lessons; full objective coverage of all four certifications |
| Quiz Engine | High | Done: graded quizzes with explanations, 70% pass mark |
| Progress Tracking | High | Done: theory/lab/quiz tracking, streaks, weekly goal, per-course progress |
| AI Mentor | Medium | Phase 3 |
| Study Planner | Medium | Done: per-certification pace check (on track / behind / overdue) and a week-by-week schedule from the weekly goal, exam dates and remaining lessons (`GET /api/plan`, Study Plan page) |
| Certification Tracking | Medium | Done: readiness by objective coverage (all 4 exams), timed practice exams with per-objective scoring, and target exam dates with a countdown on exams and progress |
| Cyber Range | Low | Phase 6 |
| Advanced Analytics | Low | Done: `GET /api/analytics` — weekly study trend with a rolling average, when-you-study patterns (weekday and part of day), learning velocity with a finish projection from the recent pace, and quiz weak-spots (weakest domains and objectives); surfaced in the desktop app as an **Insights** page (trend and weekday charts, velocity summary, quiz weak-spots) |
| Graded labs with checked answers | High | In progress: one auto-marked lab per course (12) built on realistic, safe datasets (logs, packet summaries, CloudTrail, scan exports, a Sigma rule, a firewall policy, a risk register). Answers are marked on the server, passing completes the lesson's lab, and graded labs can no longer be self-marked done. Desktop lesson page shows the files, takes answers and gives per-task feedback. Next: more labs per course and a Labs overview page (`GET /api/labs` is ready) |
| More lessons per course (target 5-10 each) | High | In progress: 84 lessons; every one of the 12 courses now has 7 (within the 5-10 target); can keep building toward the upper bound |
| Render lesson Markdown richly in the desktop app | Medium | Done: theory shows bold and inline code; paragraphs spaced (Core Markdown parser, unit-tested) |
| Invalidate existing sessions after a password change | Medium | Done: each token carries a `token_version`; a password change bumps it so older tokens are rejected, and the change returns a fresh token so the current session stays signed in |
| UI tests for XAML views and chart converters on Windows | Medium | Partly done: `CyberSecOS.Desktop.Tests` (net9.0-windows) covers all the value converters and the LiveCharts chart/axis converters, run headless on the Windows CI job. Full XAML view rendering stays a documented manual smoke test (needs an STA visual tree the CI runner can't host) |
| Start the backend automatically from the desktop app | Medium | Partly done: `Start CyberSecOS.bat` launcher in the Windows build; a single installer is not started |
| Data backup and export (PDF progress report) | Low | Partly done: `GET /api/progress/report.pdf` exports a one-page PDF progress report (overall stats, per-course progress, certification readiness). Data backup/export in other formats not started |
