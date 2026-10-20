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
| Advanced Analytics | Low | Not started |
| More lessons per course (target 5-10 each) | High | In progress: 72 lessons; every one of the 12 courses now has 6 (well within the 5-10 target); can keep building toward the upper bound |
| Render lesson Markdown richly in the desktop app | Medium | Done: theory shows bold and inline code; paragraphs spaced (Core Markdown parser, unit-tested) |
| Invalidate existing sessions after a password change | Medium | Done: each token carries a `token_version`; a password change bumps it so older tokens are rejected, and the change returns a fresh token so the current session stays signed in |
| UI tests for XAML views and chart converters on Windows | Medium | Partly done: `CyberSecOS.Desktop.Tests` (net9.0-windows) covers all the value converters and the LiveCharts chart/axis converters, run headless on the Windows CI job. Full XAML view rendering stays a documented manual smoke test (needs an STA visual tree the CI runner can't host) |
| Start the backend automatically from the desktop app | Medium | Partly done: `Start CyberSecOS.bat` launcher in the Windows build; a single installer is not started |
| Data backup and export (PDF progress report) | Low | Partly done: `GET /api/progress/report.pdf` exports a one-page PDF progress report (overall stats, per-course progress, certification readiness). Data backup/export in other formats not started |
