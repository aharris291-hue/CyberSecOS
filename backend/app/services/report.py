"""Render a learner's progress as a one-page PDF they can download and share."""

from __future__ import annotations

from datetime import date
from io import BytesIO

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm

INK = colors.HexColor("#1f2933")
MUTED = colors.HexColor("#616e7c")
ACCENT = colors.HexColor("#2563eb")
RULE = colors.HexColor("#d9dee4")
BAND = colors.HexColor("#f2f5f8")


def _format_hours(minutes: int) -> str:
    """A compact 'Xh Ym' from a count of minutes (e.g. 95 -> '1h 35m')."""
    hours, mins = divmod(max(minutes, 0), 60)
    if hours and mins:
        return f"{hours}h {mins}m"
    if hours:
        return f"{hours}h"
    return f"{mins}m"


def build_progress_pdf(
    *,
    display_name: str,
    target_role: str | None,
    totals: dict,
    week: dict,
    courses: list[dict],
    certifications: list[dict],
    generated_on: date,
) -> bytes:
    """Build the progress report PDF and return its bytes.

    Takes the same figures the ``/api/progress`` endpoint serves, so the report
    and the on-screen progress always agree.
    """
    from reportlab.platypus import (
        SimpleDocTemplate,
        Paragraph,
        Spacer,
        Table,
        TableStyle,
    )

    styles = getSampleStyleSheet()
    title_style = ParagraphStyle(
        "ReportTitle", parent=styles["Title"], textColor=INK, fontSize=20, spaceAfter=2
    )
    subtitle_style = ParagraphStyle(
        "ReportSubtitle", parent=styles["Normal"], textColor=MUTED, fontSize=10, spaceAfter=0
    )
    heading_style = ParagraphStyle(
        "SectionHeading",
        parent=styles["Heading2"],
        textColor=ACCENT,
        fontSize=13,
        spaceBefore=16,
        spaceAfter=6,
    )

    buffer = BytesIO()
    doc = SimpleDocTemplate(
        buffer,
        pagesize=A4,
        title="CyberSecOS Progress Report",
        author="CyberSecOS",
        topMargin=18 * mm,
        bottomMargin=18 * mm,
        leftMargin=18 * mm,
        rightMargin=18 * mm,
    )

    story: list = [Paragraph("CyberSecOS Progress Report", title_style)]
    who = display_name
    if target_role:
        who += f" &middot; {target_role}"
    story.append(Paragraph(who, subtitle_style))
    story.append(Paragraph(f"Generated {generated_on.strftime('%d %b %Y')}", subtitle_style))

    # --- Overall -----------------------------------------------------------
    story.append(Paragraph("Overall", heading_style))
    avg = totals.get("average_quiz_score")
    overview = [
        ["Lessons completed", f"{totals['completed_lessons']} of {totals['total_lessons']}"],
        ["Completion", f"{totals['completion_percent']}%"],
        ["Labs completed", str(totals["labs_completed"])],
        ["Quizzes passed", str(totals["quizzes_passed"])],
        ["Average quiz score", f"{avg}%" if avg is not None else "No quizzes yet"],
        ["Total study time", _format_hours(totals["total_minutes"])],
        ["Hands-on share", f"{totals['hands_on_percent']}%" if totals.get("hands_on_percent") is not None else "N/A"],
        ["Current streak", f"{totals['current_streak_days']} days"],
        ["Longest streak", f"{totals['longest_streak_days']} days"],
        ["This week", f"{_format_hours(week['minutes'])} of {_format_hours(week['goal_minutes'])} goal ({week['goal_percent']}%)"],
    ]
    overview_table = Table(overview, colWidths=[55 * mm, 119 * mm], hAlign="LEFT")
    overview_table.setStyle(
        TableStyle(
            [
                ("FONTSIZE", (0, 0), (-1, -1), 10),
                ("TEXTCOLOR", (0, 0), (0, -1), MUTED),
                ("TEXTCOLOR", (1, 0), (1, -1), INK),
                ("TOPPADDING", (0, 0), (-1, -1), 4),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
                ("LINEBELOW", (0, 0), (-1, -2), 0.5, RULE),
            ]
        )
    )
    story.append(overview_table)

    # --- Courses -----------------------------------------------------------
    story.append(Paragraph("Courses", heading_style))
    course_rows = [["Course", "Completed", "Progress"]]
    for course in courses:
        course_rows.append(
            [
                course["title"],
                f"{course['completed_lessons']} / {course['total_lessons']}",
                f"{course['percent']}%",
            ]
        )
    course_table = Table(course_rows, colWidths=[114 * mm, 30 * mm, 30 * mm], hAlign="LEFT")
    course_table.setStyle(_grid_style(len(course_rows)))
    story.append(course_table)

    # --- Certification readiness ------------------------------------------
    story.append(Paragraph("Certification readiness", heading_style))
    cert_rows = [["Certification", "Objectives", "Readiness", "Exam date"]]
    for cert in certifications:
        if cert.get("target_date"):
            days = cert.get("days_until_exam")
            when = cert["target_date"]
            if days is not None:
                when += f" ({days}d)" if days >= 0 else f" (overdue {abs(days)}d)"
        else:
            when = "Not set"
        cert_rows.append(
            [
                f"{cert['name']} ({cert['exam_code']})",
                f"{cert['objectives_mastered']} / {cert['objectives_total']}",
                f"{cert['readiness_percent']}%",
                when,
            ]
        )
    cert_table = Table(cert_rows, colWidths=[74 * mm, 30 * mm, 28 * mm, 42 * mm], hAlign="LEFT")
    cert_table.setStyle(_grid_style(len(cert_rows)))
    story.append(cert_table)

    story.append(Spacer(1, 10 * mm))
    story.append(
        Paragraph(
            "Readiness is the share of a certification's exam objectives covered by lessons you have completed.",
            subtitle_style,
        )
    )

    doc.build(story)
    return buffer.getvalue()


def _grid_style(row_count: int):
    from reportlab.platypus import TableStyle

    return TableStyle(
        [
            ("FONTSIZE", (0, 0), (-1, -1), 10),
            ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
            ("BACKGROUND", (0, 0), (-1, 0), ACCENT),
            ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
            ("TEXTCOLOR", (0, 1), (-1, -1), INK),
            ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, BAND]),
            ("ALIGN", (1, 0), (-1, -1), "CENTER"),
            ("ALIGN", (0, 0), (0, -1), "LEFT"),
            ("TOPPADDING", (0, 0), (-1, -1), 5),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
            ("LEFTPADDING", (0, 0), (-1, -1), 8),
            ("RIGHTPADDING", (0, 0), (-1, -1), 8),
            ("LINEBELOW", (0, 0), (-1, -1), 0.5, RULE),
        ]
    )
