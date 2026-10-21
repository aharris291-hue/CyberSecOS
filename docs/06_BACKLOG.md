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
| More lessons per course (target ~300 hours total) | High | In progress: 108 lessons (~119 hours); every one of the 12 courses now has 9. The goal is now ~300 hours of lessons in total (~273 lessons, about 66 min each), spread evenly across the 12 courses and steered by each certification's official exam objectives. The earlier 5-10-per-course guideline is superseded by this hours target; the per-lesson quality bar is unchanged |
| Graded labs with checked answers | High | In progress: 48 auto-marked labs, four per course, on realistic and safe datasets. Answers are marked on the server and passing completes the lesson's lab; graded labs cannot be self-marked. The desktop lesson page shows the files, takes answers and gives feedback per task, and a **Labs** page lists every lab by course with its status and best score. Next: a fifth lab per course, and labs that use Sysmon/EVTX-style and PCAP-style data more deeply |
| More lessons per course (target 5-10 each) | High | In progress: 84 lessons; every one of the 12 courses now has 7 (within the 5-10 target); can keep building toward the upper bound |
| More lessons per course (target ~300 hours total) | High | Built out: 258 lessons (~283 hours) on a deliberate per-course split (weighted by certification-objective coverage and genuinely distinct material). Each course was taken to the point its distinct SOC material ran out rather than padded: Threat Hunting 18, Detection Engineering 19, SIEM/GRC 20, Networking/Linux/Vulnerability Management 22, Security Foundations/Windows/Incident Response/Security Architecture/Cloud 23. The realistic distinct-material ceiling (~283h) is a little short of the literal 300h; closing the last gap would repeat topics, so the build stopped at genuinely distinct coverage. Next: audit the catalog against the official exam objectives and list any gaps. The goal is ~300 hours of lessons in total (~273 lessons, about 66 min each), spread evenly across the 12 courses and steered by each certification's official exam objectives. The earlier 5-10-per-course guideline is superseded by this hours target; the per-lesson quality bar is unchanged. Each batch also checks new lessons against existing topics so the catalog grows without duplicating lessons |
| Render lesson Markdown richly in the desktop app | Medium | Done: theory shows bold and inline code; paragraphs spaced (Core Markdown parser, unit-tested) |
| Invalidate existing sessions after a password change | Medium | Done: each token carries a `token_version`; a password change bumps it so older tokens are rejected, and the change returns a fresh token so the current session stays signed in |
| UI tests for XAML views and chart converters on Windows | Medium | Partly done: `CyberSecOS.Desktop.Tests` (net9.0-windows) covers all the value converters and the LiveCharts chart/axis converters, run headless on the Windows CI job. Full XAML view rendering stays a documented manual smoke test (needs an STA visual tree the CI runner can't host) |
| Start the backend automatically from the desktop app | Medium | Done: the API is bundled as a standalone `cybersecos-server.exe` (PyInstaller, no Python needed) that the desktop app starts on launch (reusing a server that already answers, moving to a free port if 8000 is taken) and stops on exit; the server also exits if the app is killed |
| Single Windows installer | Medium | Done: `installer/CyberSecOS.iss` (Inno Setup) builds `CyberSecOS-Setup-<version>.exe` on the Windows CI job; per-user install with Start menu and optional desktop shortcuts, no administrator prompt. Not yet code-signed (SmartScreen warns) |
| Data backup and export (PDF progress report) | Low | Partly done: `GET /api/progress/report.pdf` exports a one-page PDF progress report (overall stats, per-course progress, certification readiness). Data backup/export in other formats not started |
