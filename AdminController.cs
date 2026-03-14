using AliceTrainingSystem.Data;
using AliceTrainingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AliceTrainingSystem.Controllers;

[Authorize]
public class CertificateController : Controller
{
    private readonly AppDbContext _db;

    public CertificateController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Course(int courseId)
    {
        var userId = User.GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var user = await _db.Users.FirstAsync(x => x.Id == userId.Value);
        var course = await _db.Courses.FirstOrDefaultAsync(x => x.Id == courseId && x.IsPublished);
        var quiz = await _db.CourseQuizzes.FirstOrDefaultAsync(x => x.CourseId == courseId && x.IsPublished);
        if (course == null || quiz == null) return NotFound();

        var lessonIds = await _db.Lessons.Where(x => x.Module!.CourseId == courseId).Select(x => x.Id).ToListAsync();
        var completedCount = await _db.LessonProgressItems.CountAsync(x => x.UserId == userId.Value && x.Completed && lessonIds.Contains(x.LessonId));
        var bestPass = await _db.QuizAttempts
            .Where(x => x.UserId == userId.Value && x.CourseQuizId == quiz.Id && x.Passed)
            .OrderByDescending(x => x.ScorePercent)
            .ThenByDescending(x => x.AttemptedAtUtc)
            .FirstOrDefaultAsync();

        if (lessonIds.Count == 0 || completedCount < lessonIds.Count || bestPass == null)
        {
            TempData["Error"] = "You must complete all lessons and pass the quiz before downloading the certificate.";
            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        return View(new CertificateViewModel
        {
            LearnerName = user.FullName,
            CourseTitle = course.Title,
            IssuedAtUtc = bestPass.AttemptedAtUtc,
            FinalScorePercent = bestPass.ScorePercent
        });
    }
}
