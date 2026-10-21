# Changelog

## Phase 2 - Advanced learning analytics

### See how your studying is trending over time
- Added `GET /api/analytics`, which looks across the learner's history rather than the current snapshot the dashboard shows: a weekly study trend (minutes, active days and a four-week rolling average), when-you-study patterns (minutes by weekday and by part of the day, with the peak), learning velocity (lessons completed in the last four weeks, the resulting per-week pace and a projected finish date at that actual pace), and quiz weak-spots (the domains and objectives with the lowest average quiz scores, to point revision).
- Read-only and derived live from the same activity, progress and quiz records, so the numbers always agree with the rest of the app. Accepts `tz_offset_minutes` and a `weeks` window (1–52, default 8).
- 8 new backend tests cover the trend bucketing and rolling average, the patterns, the velocity projection, the quiz trend and weak-area ranking, the empty-account payload and the `weeks` bound (416 backend tests).

## Phase 2 - Tests for the desktop converters

### Cover the WPF value and chart converters
- Added `CyberSecOS.Desktop.Tests` (net9.0-windows), a new test project that covers every WPF value converter (visibility, inverse-bool, status label and brush selection logic, Markdown-stripping, enum matching, exam countdown) and the LiveCharts chart and axis converters (series mapping for stacked/row/pie, zero-sum filtering, category and value axes). It runs headless on the Windows CI job, which already does `dotnet test` on the whole solution.
- Documented the remaining gap honestly: full XAML view rendering stays a manual Windows smoke test, because instantiating the views needs an STA visual tree the headless CI runner can't host. The converter logic those views bind to is now covered.

## Phase 2 - Downloadable PDF progress report

### Export your progress as a PDF
- Added `GET /api/progress/report.pdf`, which returns a one-page PDF a learner can download and share: overall stats (completion, labs, quizzes, average score, study time, streaks, this week vs goal), per-course progress, and certification readiness with exam-date countdowns.
- The report is built from the same `ProgressStats` figures as `/api/progress`, so the document and the on-screen numbers always agree, and it renders correctly for a brand-new account with no activity yet.
- Adds `reportlab` to the backend requirements. 7 new backend tests cover the endpoint and the report builder (408 backend tests).

## Phase 2 - A sixth lesson for the last four courses

### A sixth lesson for Security Foundations, Security Architecture, Cloud Security and Governance/Risk/Compliance
- Finished the second pass past five lessons: the last four courses each gain a sixth lesson, so every one of the twelve courses now has six (72 lessons total).
- New lessons: malware types and how they behave (families, matching behaviour to type, fileless and living-off-the-land); designing key management and secrets architecture (where keys and secrets live, their lifecycle and rotation, least-privilege access); securing containers and serverless workloads (build/registry/runtime risk, image scanning and signing, least-privilege workload identity); and security metrics and reporting to leadership (decision-driving metrics over vanity numbers, risk in business terms, honest reporting).
- All keep the six education-standard sections and the ~18/82 theory/lab split (catalog theory ratio 0.18); content-standard, seeding and coverage tests still pass (401 backend tests).

## Phase 2 - A sixth lesson for Networking, Windows, Linux and Vulnerability Management

### A sixth lesson for the next tier of courses
- Continued the second pass past five lessons, adding a sixth to Networking, Windows, Linux and Vulnerability Management, so eight of the twelve courses now have six (68 lessons total).
- New lessons: flow analysis and detecting scanning and lateral movement (NetFlow vs full capture, horizontal and vertical scans, east-west movement); detecting lateral movement in Windows logs (logon types 2/3/10, the PsExec service-creation pattern, reconstructing a host-to-host path); detecting Linux privilege escalation (sudo and SUID abuse, telling abuse from legitimate administration); and validating findings and verifying remediation (false positives, safe confirmation, rescan to verify closure, risk exceptions).
- All keep the six education-standard sections and the ~18/82 theory/lab split (catalog theory ratio 0.18); content-standard, seeding and coverage tests still pass (385 backend tests).

## Phase 2 - A sixth lesson for the SOC daily-driver courses

### A sixth lesson for SIEM, Threat Hunting, Incident Response and Detection Engineering
- Opened the second pass past five lessons, adding a sixth to the four courses closest to daily SOC work (64 lessons total).
- New lessons: threat-intelligence matching in the SIEM (watchlists, indicator context and aging, matches as leads); hunting data exfiltration (stage-package-move, egress volume and destination against baseline, ruling out backups); responding to business email compromise (account takeover signs, concealment inbox rules, session revocation and rule removal); and detection-as-code and the detection lifecycle (version control, pull-request review, CI validation on known-good/known-bad samples, deprecation).
- All keep the six education-standard sections and the ~18/82 theory/lab split (catalog theory ratio 0.18); content-standard, seeding and coverage tests still pass (369 backend tests).

## Phase 2 - Every course five lessons deep

### A fifth lesson for the last four courses
- Added a fifth lesson to Security Foundations, Security Architecture, Cloud Security and Governance/Risk/Compliance, so all 12 courses now have five lessons each (60 lessons total) - the lower bound of the 5-10 lessons-per-course target.
- New lessons: identity and access management fundamentals (AAA, MFA, SSO, least privilege, the account lifecycle and IAM attack signals); designing logging and monitoring architecture (what to log, central collection, log integrity, time sync, coverage maps and silent-source alerts); cloud identity, roles and least privilege (policies and service identities, over-broad grants, privilege-escalation paths, permission boundaries); and security frameworks and control mapping (NIST CSF, ISO 27001, CIS Controls, mapping one control to many requirements, finding gaps).
- All keep the six education-standard sections and the ~18/82 theory/lab split (catalog theory ratio 0.18); content-standard, seeding and coverage tests still pass (353 backend tests).

## Phase 2 - Five lessons for the environment and tooling courses

### A fifth lesson for Networking, Windows, Linux and Vulnerability Management
- Added a fifth lesson to the next tier of courses (56 lessons total), so eight of the twelve courses now have five lessons.
- New lessons: detecting DNS abuse, tunnelling and DGA (query shape, NXDOMAIN rate, record types, encrypted-DNS bypass); detecting credential theft and LSASS access (Sysmon ProcessAccess, SAM/SYSTEM hives, DCSync, Credential Guard and LSA protection); triaging a live Linux host (volatile state in order of volatility with ps/ss/lsof and /proc, preserve-don't-reboot, timeline); and configuration and compliance scanning (CIS Benchmarks, DISA STIGs, SCAP, failed controls to owned remediation and drift tracking).
- All keep the six education-standard sections and the ~18/82 theory/lab split (catalog theory ratio 0.18); content-standard, seeding and coverage tests still pass (337 backend tests).

## Phase 2 - Five lessons for the SOC daily-driver courses

### A fifth lesson for SIEM, Threat Hunting, Incident Response and Detection Engineering
- Added a fifth lesson to the four courses closest to an analyst's daily work (52 lessons total), starting the climb from four toward the 5-10 lessons-per-course target.
- New lessons: onboarding and normalising log sources (collection, parsing, common-schema normalisation, UTC timestamps and completeness checks); hunting lateral movement and credential access (pass-the-hash, remote execution, Kerberoasting, baselines and fan-out); responding to a ransomware incident (early signs, scope-isolate-preserve-eradicate-recover, and the payment decision); and behavioural and anomaly-based detection (baselines, thresholds, UEBA, and when it beats signatures).
- All keep the six education-standard sections and the ~18/82 theory/lab split (catalog theory ratio 0.18); content-standard, seeding and coverage tests still pass (321 backend tests).

## Phase 2 - Every course four lessons deep

### A fourth lesson for the six courses still at three
- Added a fourth lesson to the remaining three-lesson courses, so all 12 courses now have four lessons each (48 lessons total).
- New lessons: analysing phishing and social engineering (email headers, SPF/DKIM/DMARC, safe link and attachment triage); finding web application vulnerabilities (OWASP Top 10, authenticated scanning, confirming findings over false positives); enriching and correlating alerts (context sources, MITRE ATT&CK mapping, deduplication); designing secure applications and APIs (least privilege, authN vs authZ, gateways, WAF, secrets); investigating a cloud incident from audit logs (CloudTrail/Activity logs, access-escalation-objective, timeline and containment); and business continuity and incident readiness (RTO/RPO, tabletop exercises, breach-notification duties).
- All keep the six education-standard sections and the ~18/82 theory/lab split (catalog theory ratio 0.18); content-standard, seeding and coverage tests still pass (305 backend tests). Also corrected the content-guide lesson-count table, which had drifted behind the seed data.

## Phase 2 - Deeper foundation courses

### A fourth lesson for Networking, Windows and Linux
- Added a fourth lesson to the three environment-and-tooling foundations a SOC analyst relies on every day (42 lessons total), taking them to the same depth as the core SOC courses.
- New lessons: reading encrypted traffic from TLS metadata (SNI, certificate fields and JA3, triaging a session without decrypting it); hunting Windows persistence in the Run keys, scheduled tasks and services, and tying each finding to its creation event (7045/4698); and command-line log triage with grep, awk and jq (the sort/uniq-c/sort-nr idiom, and querying JSON logs).
- All keep the six education-standard sections and the ~18/82 theory/lab split; content-standard, seeding and coverage tests still pass (281 backend tests). A dashboard test that assumed the first course had three lessons now derives the expected percentage from the course's lesson count.

## Phase 2 - Deeper core SOC courses

### A fourth lesson for SIEM, Threat Hunting and Incident Response
- Added a fourth lesson to the three courses closest to a SOC analyst's daily work (39 lessons total), pushing the core of the path past three lessons each.
- New lessons: SOC dashboards, metrics and shift handover (MTTD/MTTR, dwell time, carrying work across shifts); hunting living-off-the-land and process lineage (LOLBins, parent-child anomalies, behavioural detection); and incident severity, communication and reporting (severity matrix, audience-tailored updates, sourced reports).
- All keep the six education-standard sections and the ~18/82 theory/lab split; content-standard, seeding and coverage tests still pass (269 backend tests).

## Phase 2 - Every course three lessons deep

### A third lesson for the last four two-lesson courses
- Added one lesson each to Threat Hunting, Security Architecture, Cloud Security and Governance/Risk/Compliance, so all 12 courses now have at least three lessons (36 lessons total).
- New lessons: hunting command-and-control beaconing (interval, jitter, ruling out benign periodic traffic); zero trust and identity-centric access (per-request verification, least privilege, microsegmentation); hardening cloud workloads and storage (public buckets, over-broad roles, instance metadata, secrets, guardrails); and data privacy, classification and regulated data (GDPR/HIPAA/PCI DSS, lifecycle, DLP).
- All keep the six education-standard sections and the ~18/82 theory/lab split; content-standard, seeding and coverage tests still pass (257 backend tests).

## Phase 2 - More SOC-core depth

### A third lesson for the remediation and response courses
- Added one lesson each to Vulnerability Management, Incident Response and Detection Engineering (12 courses, 32 lessons), deepening the detect-respond-remediate core of the SOC path.
- New lessons: prioritising vulnerabilities with CVSS, EPSS and the CISA KEV catalog then reporting with owners and SLAs; digital forensics and evidence handling (order of volatility, hashing, chain of custody, timelines); and tuning detections to cut false positives (precision, scoped tuning vs blanket suppression, rule lifecycle).
- All keep the six education-standard sections and the ~18/82 theory/lab split; content-standard, seeding and coverage tests still pass (241 backend tests).

## Phase 2 - Session hardening

### Sign out other sessions when the password changes
- Changing the account password now invalidates every access token issued before the change, so a token that leaked or is still held on another device stops working. The session that made the change gets a fresh token back and stays signed in; the desktop app re-signs-in transparently and notes that other sessions were signed out.
- Each JWT now carries the account's `token_version`; a password change increments it and `/api/users/me/password` returns a new token (was a 204). 5 new/updated backend tests and 1 new desktop test (229 backend, 111 desktop).

## Phase 2 - Study planner

### A pace check and a weekly schedule toward each exam
- A new Study Plan page turns the weekly goal, the target exam dates and the remaining lessons into two things: a per-certification pace check (ready / on track / behind / overdue, with the hours per week needed to finish in time) and a schedule that packs the remaining lessons into weeks at the chosen pace, soonest-exam work first. Each scheduled lesson opens straight into its lesson page.
- New read-only endpoint `GET /api/plan` (derived live; nothing new is stored). 10 new backend tests and 10 new desktop view-model tests (227 backend, 110 desktop tests total).

## Phase 2 - More SOC-core depth

### A third lesson for the core environment and tooling courses
- Added one lesson each to Windows, Linux and SIEM (12 courses, 29 lessons), the environments and tool a SOC analyst works in daily.
- New lessons: Windows Active Directory and authentication monitoring (logon types, Kerberos, spraying and lateral movement); Linux auditing and persistence hunting (auditd/journald, cron/systemd/startup files, tracing a process via /proc); and investigating a SIEM alert end-to-end (triage, pivoting, timeline, verdict).
- All keep the six education-standard sections and the ~18/82 theory/lab split; content-standard and coverage tests still pass (217 backend tests).

## Phase 2 - Exam-date tracking

### Target exam dates with a countdown
- A learner can set a target sitting date for each certification and see a live countdown, both on the Practice Exams page and next to certification readiness on the Progress page.
- New `exam_targets` table and endpoints (`GET`/`PUT`/`DELETE` `/api/certifications/{cert_id}/exam/target`); certification readiness in `/api/progress` and `/api/dashboard` now carries `target_date` and `days_until_exam`.
- 8 new backend tests and 6 new desktop view-model tests (205 backend, 100 desktop tests total).

## Phase 2 - Readable lessons

### Rich Markdown rendering of lesson theory
- Lesson theory now renders real formatting in the desktop app: bold terms, inline `code`, and spaced paragraphs, instead of showing the raw `**` and backtick markers.
- A small, dependency-free Markdown reader in `CyberSecOS.Core` (paragraphs, bold, inline code) parses the theory; an attached property fills the view's text inlines. 11 new unit tests cover it (94 desktop tests total).

## Phase 2 - Deeper SOC content

### A second lesson for every single-lesson course
- Added one lesson each to SIEM, Threat Hunting, Incident Response, Detection Engineering and Cloud Security, so every course now has at least two lessons (12 courses, 26 lessons).
- New lessons cover log sources/correlation/alerting, threat-intel hypothesis-driven hunting, incident containment/recovery/lessons-learned, detection testing and ATT&CK coverage mapping, and cloud logging/posture management - the day-to-day SOC skills SC-200 and CySA+ test hardest.
- All lessons keep the six education-standard sections and the ~18/82 theory/lab split; content-standard and coverage tests still pass (197 backend tests).

## Phase 2 - Timed practice exams

### Certification practice exams
- New exam engine samples quiz questions across the lessons mapped to a certification's objectives, spread round-robin so breadth of readiness is tested rather than one lesson.
- API under `/api/certifications/{cert_id}/exam`: exam info, a reshuffled paper of 5-100 questions, scored submission, and attempt history (`12_API.md`).
- Graded at an 80% pass mark with a per-objective breakdown; each sitting is recorded in the new `exam_attempts` table with its duration and breakdown.
- Desktop "Practice Exams" page: pick a certification and length, a timed sitting, then a score with highlighted focus areas linking to courses.
- 15 backend tests and 13 desktop view-model tests cover sampling, grading, the breakdown and the exam flow.

## Phase 2 - Learning content

### Full certification coverage
- Added 4 courses and 9 lessons (Security Foundations, Vulnerability Management, Security Architecture, Governance/Risk/Compliance): 12 courses and 21 lessons total.
- Every objective of Security+, SC-900, SC-200 and CySA+ now maps to at least one lesson; catalog is ~18% theory / 82% lab.
- New tests enforce full Security+ objective coverage and full coverage of every priority certification.

## Phase 1 - Core Application (complete)

### Project structure
- Monorepo with `backend/` (FastAPI) and `frontend/` (WPF, MVVM) per the architecture document.
- Health endpoint `GET /api/health`.
- GitHub Actions CI: PyTest on Ubuntu, build and xUnit on Windows.

### Database
- SQLAlchemy models for users, certifications and objectives, courses, lessons, quizzes, progress, quiz attempts and study activity (see `11_DATA_MODEL.md`).
- Tables are created on startup; SQLite foreign keys are enforced.

### Login and user profiles
- First-run account creation and sign-in (`/api/auth/*`) with bcrypt hashing, JWT sessions and lockout after 5 failed attempts.
- Profile read/update and password change (`/api/users/me`).
- Desktop: application shell with view-model-first navigation, login/first-run page, profile page.

### Course engine and quizzes
- 8 courses (one per focus domain) and 12 lessons, each with learning objectives, theory, examples, a quiz, a practical lab and an expected outcome; 19% theory / 81% lab time.
- Certification catalog (Security+, SC-900, SC-200, CySA+) with every lesson mapped to exam objectives.
- Idempotent YAML seeding at startup; content standards enforced by tests (`13_CONTENT_GUIDE.md`).
- Quiz grading with a 70% pass mark, explanations and recorded attempts.
- Desktop: course catalog and lesson pages with an interactive quiz.

### Progress tracking
- Theory read and lab completion with notes and actual minutes; lessons complete when the lab is done and the quiz is passed.
- `/api/progress`: totals, hands-on share, streaks, weekly goal, per-course progress, certification readiness, recent activity, next lesson.
- `/api/progress/activity`: daily study minutes, bucketed in the learner's time zone.
- Desktop: lesson progress checklist, lab notes and completion, and a Progress page.

### Dashboard
- `GET /api/dashboard` returns everything the home screen needs in one call.
- Desktop dashboard (new home page): greeting, up-next lesson, headline stats, weekly goal, and LiveCharts2 charts for daily study time, theory vs hands-on, progress by domain and certification readiness.

### Testing
- End-to-end learner journey test; malformed server responses surface as friendly errors.
- CI runs on pull requests and pushes to `main` only, avoiding duplicate runs.
- Test inventory in `14_TESTING.md`.

### Windows build
- CI publishes a self-contained Windows build (`CyberSecOS-windows` artifact) with the backend and a `Start CyberSecOS.bat` launcher.
- The login screen re-checks the server when you press Sign in, so starting the API late no longer needs an app restart.
- Quick start guide: `15_WINDOWS_QUICKSTART.md`.
