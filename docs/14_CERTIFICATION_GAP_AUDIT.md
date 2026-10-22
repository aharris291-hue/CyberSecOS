# Certification Objective Gap Audit

This audit checks the lesson catalog against the official exam objectives of
the four priority certifications, and lists the remaining gaps. It was first run
on 258 lessons (~283 hours, 12 courses). The counts below are refreshed for the
current catalog: 268 lessons (~294 hours, 13 courses), after the Microsoft track
and the depth-gap pass described under each certification.

## Method and scope

- Coverage is measured at the level of each certification's **published
  top-level objectives** (the objective codes the exams list). The catalog tags
  every lesson with the objective ids it covers; this audit counts how many
  lessons map to each objective and reads that against what the objective is.
- `test_content_standards.py` already enforces that **every objective of all
  four certifications maps to at least one lesson**, and that Security+ in
  particular never loses full coverage. So there are **no objectives with zero
  coverage** — the build fails if one appears. This audit is therefore about
  **depth**: which objectives are only thinly covered, and which qualitative
  topics an exam-taker would still need beyond this catalog.
- Caveat: CompTIA objectives (Security+, CySA+) each contain many sub-bullets.
  The catalog maps to the top-level objective, not every sub-bullet, so a
  thinly-covered objective may leave specific sub-bullets untouched. Depth
  figures below are a proxy for, not a guarantee of, sub-bullet completeness.

## Headline

- **Every published objective of all four certifications is covered** by at
  least one lesson, and most are covered well. The catalog is strongest on the
  hands-on SOC-analyst core (monitoring and alerting, malicious-activity
  analysis, architecture, incident response, threat hunting), which is where
  the lesson count is deliberately concentrated.
- The **gaps are thin spots and vendor-specificity**, not missing topics:
  a handful of Security+ and CySA+ objectives rest on only one or two lessons,
  and the two Microsoft exams (SC-900, SC-200) are covered at the concept level
  but not at the Microsoft-product level the exams actually test.
- The catalog reached ~283 hours of genuinely distinct material against the
  ~300-hour target; the remaining ~17 hours would be repetition, so the build
  stopped at distinct coverage (see `13_CONTENT_GUIDE.md`).

## Security+ (SY0-701) — coverage by objective

Lessons mapped per objective (28 objectives, all covered). The three
objectives that were thin in the first audit are in bold, with their original
count in brackets:

| Obj | Lessons | Obj | Lessons | Obj | Lessons |
|---|---|---|---|---|---|
| 1.1 | 2 | 2.5 | 11 | 4.5 | 6 |
| 1.2 | 7 | 3.1 | 41 | 4.6 | 20 |
| 1.3 | 2 | 3.2 | 15 | **4.7** | **6** (1) |
| 1.4 | 10 | 3.3 | 4 | 4.8 | 34 |
| 2.1 | 4 | 3.4 | 2 | 4.9 | 10 |
| 2.2 | 5 | 4.1 | 33 | 5.1 | 24 |
| 2.3 | 10 | **4.2** | **5** (1) | 5.2 | 4 |
| 2.4 | 17 | 4.3 | 19 | **5.3** | **4** (1) |
|  |  | 4.4 | 61 | 5.4 | 13 |
|  |  |  |  | 5.5 | 5 |
|  |  |  |  | 5.6 | 3 |

**First audit: thinly covered (one lesson each).** These were the real Security+
gaps; each is now closed, see "Depth-gap pass" below.

- **4.7 — Automation and orchestration for secure operations.** Only one lesson
  (SIEM SOAR playbooks) is tagged to it. SOAR, detection-as-code and
  GRC/compliance automation all touch it but aren't tagged here. For an exam
  that explicitly tests automation benefits (scripting, workflow orchestration,
  ticketing/CI pipelines, use cases, considerations), this is the single
  thinnest spot and the best candidate for a dedicated "security automation and
  orchestration concepts" lesson.
- **4.2 — Hardware, software, and data asset management.** One lesson (the
  vulnerability-management asset lesson). Acquisition/procurement, assignment/
  accounting (ownership, classification), and monitoring/asset-tracking plus
  secure disposal/decommissioning are only lightly present. A short asset-
  management-lifecycle lesson (or re-tagging secure-config and data-governance
  lessons to 4.2) would close it.
- **5.3 — Third-party risk assessment and management.** One lesson (the GRC
  third-party risk lesson). The supply-chain security lesson is highly relevant
  but is tagged to other objectives; re-tagging it to 5.3, or a dedicated
  vendor-assessment/right-to-audit/SLA lesson, would deepen it.

**Lighter but adequate (two lessons):** 1.1 (control types), 1.3 (change
management), 3.4 (resilience and recovery). These are each genuinely covered;
1.3 (change management) is the one worth watching, since the exam gives it its
own objective and the catalog treats it mainly inside the GRC change-management
lesson.

**Depth-gap pass (closed).** Lessons whose content already taught these
objectives were re-tagged, and a distinct lesson was added only for the
sub-bullets nothing taught:

- **4.7, now 6 lessons.** Re-tagged: SIEM SOAR playbooks, detection-as-code,
  GRC compliance automation (alongside the GRC risk/automation lesson and the
  Microsoft Sentinel lesson). New: *Scripting Security Automation with Bash and
  Python* (Linux), covering the exam's use cases (provisioning and
  deprovisioning, guard rails, security groups, ticketing and escalation,
  enabling/disabling services, CI and APIs), benefits and considerations.
- **4.2, now 5 lessons.** Re-tagged: attack surface management (enumeration),
  data governance (ownership, retention, disposal) and data classification.
  New: *Asset Lifecycle: Procurement, Inventory and Secure Disposal* (Security
  Foundations), covering procurement, assignment and accounting, CMDB
  reconciliation, NIST SP 800-88 clear/purge/destroy and certificates of
  destruction.
- **5.3, now 4 lessons.** Re-tagged: supply-chain security and SOC 2
  attestation (reading a vendor's report). New: *Vendor Assessment, Selection
  and Third-Party Agreements* (GRC), covering tiering, questionnaires,
  independent assessments, right to audit, due diligence and conflicts of
  interest, SLA/MOA/MOU/MSA/SOW/NDA/BPA, monitoring, rules of engagement and
  offboarding.

**Heaviest coverage** (as intended for a SOC-analyst catalog): 4.4 security
alerting and monitoring (61), 3.1 architecture models (41), 4.8 incident
response (34), 4.1 securing compute resources (33), 5.1 governance (24).

## CySA+ (CS0-003) — coverage by objective

| Obj | Lessons | Obj | Lessons | Obj | Lessons |
|---|---|---|---|---|---|
| 1.1 | 45 | 2.2 | 5 | 3.3 | 5 |
| 1.2 | 33 | 2.3 | 4 | 4.1 | 16 |
| 1.3 | 46 | **2.4** | **8** (2) | 4.2 | 6 |
| 1.4 | 40 | 2.5 | 7 |  |  |
| 1.5 | 8 | 3.1 | 28 |  |  |
| 2.1 | 17 | 3.2 | 20 |  |  |

CySA+ is the best-covered exam: its Domain 1 (security operations: architecture,
malicious-activity analysis, tools/techniques, threat intel and hunting) is the
catalog's centre of gravity, and incident response (3.1, 3.2) is deep.

**Thinnest spot in the first audit (now closed):**

- **2.4 — Recommend controls to mitigate attacks and software vulnerabilities.**
  Two lessons. Mitigation/remediation is present across the vulnerability-
  management course, but the specific "given a vulnerability or attack, recommend
  the control" framing is lightly tagged. Re-tagging the remediation, secure-
  coding and compensating-control content to 2.4, or a dedicated
  mitigation-recommendation lesson, would close it.
  **Closed, now 8 lessons.** Re-tagged: web application scanning (OWASP
  classes), secure application and API design, WAF and API gateway (virtual
  patching), patch remediation (compensating controls) and web server
  hardening. New: *Recommending Controls for Common Attacks and
  Vulnerabilities* (Vulnerability Management), which pairs each CS0-003 attack
  class (injection, XSS, CSRF, SSRF, traversal and file inclusion, overflows,
  broken access control, cryptographic failures, data poisoning, privilege
  escalation, RCE) with its root-cause fix, layered controls and a time-bound
  compensating control.
- Moderately light: 2.2 (analyze scan output, 5), 2.3 (prioritize
  vulnerabilities, 4), 3.3 (preparation and post-incident phases, 5). All are
  covered by named lessons; worth a second lesson each only if depth is desired.

## SC-900 and SC-200 (Microsoft) — the qualitative gap

Both Microsoft exams map fully at the domain level:

- **SC-900:** 1 (security/compliance/identity concepts) 12, 2 (Microsoft
  Entra) 11, 3 (Microsoft security solutions) 13, 4 (Microsoft compliance
  solutions) 24.
- **SC-200:** 1 (manage the SecOps environment) 19, 2 (configure protections
  and detections) 53, 3 (manage incident response) 17, 4 (threat hunting) 27.

**The gap here is vendor specificity, not concepts.** These two exams are
heavily tied to specific Microsoft products, and the catalog is deliberately
vendor-neutral. A learner gets the underlying concepts (identity, conditional
access, EDR/XDR, SIEM, DLP/compliance, hunting, KQL), but the exams test the
**Microsoft product surface** by name:

- **SC-900:** Microsoft Entra ID (and Entra features — conditional access,
  PIM, identity protection), Microsoft Defender XDR family, Microsoft Sentinel,
  Microsoft Purview (compliance, DLP, information protection), the Microsoft
  Service Trust Portal and licensing/service-model specifics.
- **SC-200:** operating Microsoft Defender XDR and Defender for
  Endpoint/Identity/Office 365/Cloud Apps, and Microsoft Sentinel end to end
  (data connectors, analytics rules, workbooks, playbooks/automation, KQL
  hunting). The catalog teaches KQL and these capabilities generically but not
  the Microsoft-portal workflows the exam expects.

**Recommendation:** either add a short Microsoft-specific track (a handful of
lessons naming Entra, Defender XDR, Sentinel and Purview and mapping the generic
skills onto them), or state plainly in the product that SC-900/SC-200 readiness
needs supplementary Microsoft-product study on top of this catalog. This is the
most consequential gap for those two certifications and the clearest honest
limit of a vendor-neutral catalog.

**Update (addressed):** a dedicated course 13, *Microsoft Security Operations
(SC-900 and SC-200)*, has since been added to close this gap — six
product-specific lessons (the Microsoft stack, Entra ID identity and governance,
Defender XDR, Microsoft Sentinel, Microsoft Purview) with four graded labs on
Entra sign-in/Conditional Access data, a Defender XDR incident, Sentinel rule
tuning and Purview DLP alerts. The vendor-neutral courses still teach the
underlying concepts; this track names the Microsoft product surface the exams
test.

## Summary of gaps to close (prioritised)

1. **SC-900 / SC-200 Microsoft-product specificity** — biggest gap; **addressed**
   by the new course 13, a Microsoft-specific track (Entra, Defender XDR,
   Sentinel, Purview) with four graded labs.
2. **Security+ 4.7 (automation and orchestration)** — **addressed**: 1 → 6
   lessons (three re-tags plus a new security scripting lesson).
3. **Security+ 4.2 (asset management)** and **5.3 (third-party risk)** —
   **addressed**: 4.2 1 → 5 (three re-tags plus a new asset lifecycle lesson),
   5.3 1 → 4 (two re-tags plus a new vendor assessment and agreements lesson).
4. **CySA+ 2.4 (recommend mitigating controls)** — **addressed**: 2 → 8 (five
   re-tags plus a new mitigation-recommendation lesson).
5. **Hours vs target** — the vendor-neutral courses reached ~283h of distinct
   material; with the Microsoft track and the four depth-gap lessons the
   catalog is ~294h. Each addition taught material no lesson covered, so the
   "no topic repeats" rule still holds.

**Still worth watching** (covered, but by two lessons): Security+ 1.1 (control
types), 1.3 (change management) and 3.4 (resilience and recovery), and CySA+
2.3 (prioritise vulnerabilities, 4). These are adequately taught today; they are
the next candidates if more depth is wanted.

None of the original gaps were missing-objective gaps. Every objective is covered,
and the four thin spots have now been deepened, mostly by re-tagging lessons that
already taught the material and adding a lesson only where nothing did.
