using AliceTrainingSystem.Data;
using AliceTrainingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AliceTrainingSystem.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _db.Users.FirstAsync(x => x.Id == userId.Value);
        var enrolledCourseIds = await _db.Enrollments
            .Where(x => x.UserId == userId.Value)
            .Select(x => x.CourseId)
            .ToListAsync();

        var courseData = await _db.Courses
            .Where(x => enrolledCourseIds.Contains(x.Id) && x.IsPublished)
            .OrderBy(x => x.SortOrder)
            .Select(course => new
            {
                course.Id,
                course.Title,
                course.Summary,
                course.HeroText,
                LessonIds = course.Modules.SelectMany(m => m.Lessons).Select(l => l.Id).ToList()
            })
            .ToListAsync();

        var completedLessonIds = await _db.LessonProgressItems
            .Where(x => x.UserId == userId.Value && x.Completed)
            .Select(x => x.LessonId)
            .ToListAsync();

        var passedQuizCourseIds = await _db.QuizAttempts
            .Where(x => x.UserId == userId.Value && x.Passed)
            .Select(x => x.CourseQuiz!.CourseId)
            .Distinct()
            .ToListAsync();

        var model = new DashboardViewModel
        {
            UserFullName = user.FullName,
            EnrolledCourseCount = courseData.Count,
            CompletedLessonCount = completedLessonIds.Count,
            CertificatesEarnedCount = courseData.Count(course => course.LessonIds.Count > 0 && course.LessonIds.All(id => completedLessonIds.Contains(id)) && passedQuizCourseIds.Contains(course.Id)),
            Courses = courseData.Select(course =>
            {
                var lessonCount = course.LessonIds.Count;
                var completed = course.LessonIds.Count(id => completedLessonIds.Contains(id));
                var percent = lessonCount == 0 ? 0 : (int)Math.Round((double)completed * 100 / lessonCount);

                return new CourseListItemViewModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    Summary = course.Summary,
                    HeroText = course.HeroText,
                    LessonCount = lessonCount,
                    CompletedLessons = completed,
                    ProgressPercent = percent,
                    IsEnrolled = true
                };
            }).ToList()
        };

        return View(model);
    }
}
