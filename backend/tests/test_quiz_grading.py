import pytest

from app.models import QuizQuestion
from app.services.quiz import PASS_MARK, grade


def _questions(n=4):
    return [QuizQuestion(id=i, order=i, prompt="p", options=["a", "b", "c"], correct_index=1, explanation="e") for i in range(n)]


def test_all_correct_scores_100():
    result = grade(_questions(), [1, 1, 1, 1])
    assert (result.score, result.correct, result.passed) == (100, 4, True)


def test_score_is_rounded_percentage():
    result = grade(_questions(3), [1, 1, 0])
    assert result.score == 67
    assert not result.passed


def test_pass_mark_boundary():
    questions = _questions(10)
    assert grade(questions, [1] * 7 + [0] * 3).passed
    assert not grade(questions, [1] * 6 + [0] * 4).passed
    assert PASS_MARK == 70


def test_unanswered_counts_as_wrong():
    result = grade(_questions(), [1, None, 1, 1])
    assert result.correct == 3
    assert result.results[1].is_correct is False
    assert result.results[1].correct_index == 1


def test_answer_count_must_match():
    with pytest.raises(ValueError):
        grade(_questions(), [1, 1])
