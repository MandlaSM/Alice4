using AliceTrainingSystem.Data;
using AliceTrainingSystem.Models;
using AliceTrainingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AliceTrainingSystem.Controllers;

[Authorize]
public class QuizController : Controller
{
    private readonly AppDbContext _db;

    public QuizController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Take(int courseId)
    {
        var userId = User.GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var quiz = await _db.CourseQuizzes
            .Include(x => x.Course)
            .Include(x => x.Questions.OrderBy(q => q.SortOrder))
                .ThenInclude(q => q.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(x => x.CourseId == courseId && x.IsPublished);

        if (quiz?.Course == null) return NotFound();

        var courseLessonIds = await _db.Lessons.Where(x => x.Module!.CourseId == courseId).Select(x => x.Id).ToListAsync();
        var completedCount = await _db.LessonProgressItems.CountAsync(x => x.UserId == userId.Value && x.Completed && courseLessonIds.Contains(x.LessonId));
        if (courseLessonIds.Count == 0 || completedCount < courseLessonIds.Count)
        {
            TempData["Error"] = "Please complete all lessons before taking the quiz.";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        var model = new QuizTakeViewModel
        {
            CourseId = courseId,
            QuizId = quiz.Id,
            CourseTitle = quiz.Course.Title,
            QuizTitle = quiz.Title,
            Summary = quiz.Summary,
            PassMarkPercent = quiz.PassMarkPercent,
            Questions = quiz.Questions.OrderBy(q => q.SortOrder).Select(q => new QuizQuestionViewModel
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                Options = q.Options.OrderBy(o => o.SortOrder).Select(o => new QuizOptionViewModel { Id = o.Id, OptionText = o.OptionText }).ToList()
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(QuizTakeViewModel model)
    {
        var userId = User.GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var quiz = await _db.CourseQuizzes
            .Include(x => x.Course)
            .Include(x => x.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(x => x.Id == model.QuizId && x.CourseId == model.CourseId && x.IsPublished);

        if (quiz?.Course == null) return NotFound();

        var orderedQuestions = quiz.Questions.OrderBy(q => q.SortOrder).ToList();
        if (orderedQuestions.Count == 0)
        {
            TempData["Error"] = "This quiz does not have any questions yet.";
            return RedirectToAction("Details", "Courses", new { id = model.CourseId });
        }

        var correctAnswers = 0;
        foreach (var question in orderedQuestions)
        {
            if (!model.Answers.TryGetValue(question.Id, out var selectedOptionId)) continue;
            var correctOption = question.Options.FirstOrDefault(x => x.IsCorrect);
            if (correctOption?.Id == selectedOptionId) correctAnswers++;
        }

        var scorePercent = (int)Math.Round(correctAnswers * 100.0 / orderedQuestions.Count);
        var passed = scorePercent >= quiz.PassMarkPercent;

        _db.QuizAttempts.Add(new QuizAttempt
        {
            CourseQuizId = quiz.Id,
            UserId = userId.Value,
            ScorePercent = scorePercent,
            Passed = passed,
            AttemptedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Result), new { courseId = model.CourseId });
    }

    public async Task<IActionResult> Result(int courseId)
    {
        var userId = User.GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var attempt = await _db.QuizAttempts
            .Include(x => x.CourseQuiz!)
                .ThenInclude(q => q.Course)
            .Where(x => x.UserId == userId.Value && x.CourseQuiz!.CourseId == courseId)
            .OrderByDescending(x => x.AttemptedAtUtc)
            .FirstOrDefaultAsync();

        if (attempt?.CourseQuiz?.Course == null) return NotFound();

        return View(new QuizResultViewModel
        {
            CourseId = courseId,
            CourseTitle = attempt.CourseQuiz.Course.Title,
            QuizTitle = attempt.CourseQuiz.Title,
            ScorePercent = attempt.ScorePercent,
            PassMarkPercent = attempt.CourseQuiz.PassMarkPercent,
            Passed = attempt.Passed,
            AttemptedAtUtc = attempt.AttemptedAtUtc
        });
    }
}
