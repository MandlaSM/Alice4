# Alice Training System - C# Milestone 2

This version extends Milestone 1 and keeps the visual feel close to the uploaded PHP training system.

## Included in Milestone 2
- Login / register / logout
- Learner dashboard
- Course catalogue, course pages, lesson pages
- Lesson completion tracking
- Final quizzes by course
- Quiz results with retake flow
- Course certificates for learners who complete all lessons and pass the quiz
- Admin course builder for:
  - courses
  - modules
  - lessons
  - quizzes
  - quiz questions
- SQLite database with seeded demo data

## Demo accounts
- Admin: `admin@alice.local` / `Admin123!`
- Learner: `learner@alice.local` / `Learner123!`

## Notes
- SQLite is used for easy local and DevOps testing.
- The database is created automatically on first run.
- If you previously ran the Milestone 1 package in the same folder, remove the old SQLite database file before testing this package so the new quiz tables are created cleanly.

## Suggested next step
Milestone 3 can add:
- richer admin editing and delete flows
- resources / events / news sections
- file upload support for lesson materials
- role/reporting enhancements
- Azure DevOps deployment polish
