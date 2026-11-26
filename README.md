# EduTrack

A web app for running a course: departments, instructors, courses, students, enrollments, assignments and submissions. ASP.NET Core MVC, EF Core with SQL Server, Identity. The UI is Arabic (right to left).

## What it does

- Three roles: Admin, Instructor, Student
- Manage departments, instructors (with photo), courses (with thumbnail) and students
- Students can enroll in several courses (many-to-many)
- Instructors post assignments with a PDF, a due date and a max grade
- Students upload their solution. Instructors grade it and leave notes.
- Deleting an instructor or course is blocked while it still has courses, students or tasks attached
- `FileStorageService` checks file type and size before saving uploads
- `GET /health` returns `{"status":"ok","database":"connected"}`, or 503 if the database is down

## Run it

You need the .NET 10 SDK and SQL Server (Express or LocalDB).

```bash
dotnet restore
dotnet run --project edutrack/edutrack.csproj
```

Then open http://localhost:5104. Missing migrations are applied on startup, and the roles and admin account are created. There is no sample data.

The development connection string is in `edutrack/appsettings.Development.json`. On a server, set `ConnectionStrings__DefaultConnection` as an environment variable instead of committing it.

The admin email and default password come from the `SeedAdmins` settings. The development default password is public in this repo, so set `SeedAdmins__DefaultPassword` before deploying anywhere real.

Mahmoud Saleh
