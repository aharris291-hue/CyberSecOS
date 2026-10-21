# Certification Objective Gap Audit

This audit checks the lesson catalog (258 lessons, ~283 hours across 12 courses)
against the official exam objectives of the four priority certifications, and
lists the remaining gaps. It is the "thread check against each certification's
objectives" step of the catalog build-out.

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

Lessons mapped per objective (28 objectives, all covered):

| Obj | Lessons | Obj | Lessons | Obj | Lessons |
|---|---|---|---|---|---|
| 1.1 | 2 | 2.5 | 11 | 4.5 | 6 |
| 1.2 | 5 | 3.1 | 41 | 4.6 | 18 |
| 1.3 | 2 | 3.2 | 15 | 4.7 | **1** |
| 1.4 | 10 | 3.3 | 3 | 4.8 | 33 |
| 2.1 | 4 | 3.4 | 2 | 4.9 | 10 |
| 2.2 | 5 | 4.1 | 33 | 5.1 | 24 |
| 2.3 | 9 | 4.2 | **1** | 5.2 | 4 |
| 2.4 | 17 | 4.3 | 19 | 5.3 | **1** |
| 2.5 | 11 | 4.4 | 60 | 5.4 | 11 |
|  |  |  |  | 5.5 | 4 |
|  |  |  |  | 5.6 | 3 |

**Thinly covered (one lesson each) — the real Security+ gaps:**

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

**Heaviest coverage** (as intended for a SOC-analyst catalog): 4.4 security
alerting and monitoring (60), 3.1 architecture models (41), 4.1 securing
compute resources (33), 4.8 incident response (33), 5.1 governance (24).

## CySA+ (CS0-003) — coverage by objective

| Obj | Lessons | Obj | Lessons | Obj | Lessons |
|---|---|---|---|---|---|
| 1.1 | 45 | 2.2 | 5 | 3.3 | 5 |
| 1.2 | 33 | 2.3 | 4 | 4.1 | 16 |
| 1.3 | 45 | 2.4 | **2** | 4.2 | 6 |
| 1.4 | 40 | 2.5 | 7 |  |  |
| 1.5 | 8 | 3.1 | 28 |  |  |
| 2.1 | 16 | 3.2 | 19 |  |  |

CySA+ is the best-covered exam: its Domain 1 (security operations: architecture,
malicious-activity analysis, tools/techniques, threat intel and hunting) is the
catalog's centre of gravity, and incident response (3.1, 3.2) is deep.

**Thinnest spot:**

- **2.4 — Recommend controls to mitigate attacks and software vulnerabilities.**
  Two lessons. Mitigation/remediation is present across the vulnerability-
  management course, but the specific "given a vulnerability or attack, recommend
  the control" framing is lightly tagged. Re-tagging the remediation, secure-
  coding and compensating-control content to 2.4, or a dedicated
  mitigation-recommendation lesson, would close it.
- Moderately light: 2.2 (analyze scan output, 5), 2.3 (prioritize
  vulnerabilities, 4), 3.3 (preparation and post-incident phases, 5). All are
  covered by named lessons; worth a second lesson each only if depth is desired.

## SC-900 and SC-200 (Microsoft) — the qualitative gap

Both Microsoft exams map fully at the domain level:

- **SC-900:** 1 (security/compliance/identity concepts) 9, 2 (Microsoft Entra)
  9, 3 (Microsoft security solutions) 12, 4 (Microsoft compliance solutions) 23.
- **SC-200:** 1 (manage the SecOps environment) 17, 2 (configure protections
  and detections) 50, 3 (manage incident response) 16, 4 (threat hunting) 26.

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
2. **Security+ 4.7 (automation and orchestration)** — add or re-tag to give it
   more than one lesson.
3. **Security+ 4.2 (asset management)** and **5.3 (third-party risk)** — one
   lesson each; add a lesson or re-tag existing relevant content (supply chain,
   secure config, data governance).
4. **CySA+ 2.4 (recommend mitigating controls)** — lightly tagged; re-tag
   remediation/secure-coding content or add a lesson.
5. **Hours vs target** — catalog is at ~283h of distinct material against the
   ~300h target; the remaining ~17h would be repetition. Treat ~283h as the
   realistic ceiling for genuinely distinct SOC content, or relax the "no topic
   repeats" rule if the literal 300h is required.

None of these are missing-objective gaps — every objective is covered and most
deeply. They are depth and vendor-coverage refinements, several of which can be
closed by re-tagging existing lessons rather than writing new ones.
