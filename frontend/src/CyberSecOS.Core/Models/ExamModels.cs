namespace CyberSecOS.Core.Models;

public sealed record ExamInfo(
    string CertificationId,
    string Name,
    string ExamCode,
    int AvailableQuestions,
    int DefaultSize,
    int PassMark);

public sealed record ExamPaper(
    string CertificationId,
    string Name,
    string ExamCode,
    int PassMark,
    IReadOnlyList<QuizQuestion> Questions);

public sealed record ExamAnswer(int QuestionId, int? SelectedIndex);

public sealed record ExamSubmission(IReadOnlyList<ExamAnswer> Answers, int? DurationSeconds);

public sealed record ObjectiveScore(string Code, string Title, int Correct, int Total, int Percent);

public sealed record ExamResult(
    string CertificationId,
    int Score,
    int Correct,
    int Total,
    bool Passed,
    int PassMark,
    IReadOnlyList<ObjectiveScore> Breakdown);

public sealed record ExamAttempt(
    int Id,
    string CertificationId,
    int Score,
    int Correct,
    int Total,
    bool Passed,
    int? DurationSeconds,
    DateTime CreatedAt);

public sealed record ExamTargetUpdate(DateOnly ExamDate);

public sealed record ExamTarget(string CertificationId, DateOnly ExamDate, int DaysUntilExam);
