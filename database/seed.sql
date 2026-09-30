/*
  Stand-alone seed script. It inserts the same 13 rows as the EF migration "SeedSampleTasks",
  for anyone who prefers running SQL. Safe to run more than once: existing ids are skipped.
  Row 13 is soft-deleted on purpose; it must never appear in lists or the summary.

  Prerequisite: the schema exists (run the API once, or `dotnet ef database update`).
*/
USE TaskFlow;
GO

-- Required for writes to tables with filtered indexes (IX_Tasks_*). sqlcmd defaults this OFF.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

SET IDENTITY_INSERT dbo.Tasks ON;

MERGE dbo.Tasks AS target
USING (VALUES
    ( 1, N'Set up CI pipeline for API',            N'Build, test and publish the API image on every pull request.',          1, 2, N'Priya Nair',    '2026-09-18T10:00:00', '2026-09-28T10:00:00', 0),
    ( 2, N'Migrate user reports to new schema',    N'Move ReportRuns into the reporting schema; keep a view for old queries.', 0, 3, N'Daniel Okafor', '2026-09-15T14:20:00', '2026-09-26T12:00:00', 0),
    ( 3, N'Fix date picker timezone offset',       N'Due dates shift by one day for users in UTC−5 and further west.',       1, 3, N'Marta Silva',   '2026-09-24T09:05:00', '2026-09-28T11:25:00', 0),
    ( 4, N'Write onboarding guide for new hires',  N'Cover local setup, Docker, and how we triage tasks.',                    0, 0, N'Sam Chen',      '2026-09-10T11:00:00', '2026-09-25T12:00:00', 0),
    ( 5, N'Review Q3 access permissions audit',    N'Confirm leavers no longer have access to production resources.',       2, 2, N'Aisha Rahman',  '2026-09-08T08:30:00', '2026-09-24T12:00:00', 0),
    ( 6, N'Add rate limiting to public endpoints', N'100 requests/min per key; return 429 with Retry-After.',                0, 2, N'Daniel Okafor', '2026-09-22T13:10:00', '2026-09-28T07:00:00', 0),
    ( 7, N'Update npm dependencies after audit',   N'Resolve the three high-severity advisories from npm audit.',            2, 1, N'Sam Chen',      '2026-09-14T15:45:00', '2026-09-22T12:00:00', 0),
    ( 8, N'Design empty states for dashboard',     N'No tasks, no results and no-permission variants.',                      1, 1, N'Lena Fischer',  '2026-09-21T10:30:00', '2026-09-28T11:00:00', 0),
    ( 9, N'Archive legacy SharePoint site',        N'Export to the records archive, then set the site to read-only.',        0, 0, N'Aisha Rahman',  '2026-09-01T09:00:00', '2026-09-14T12:00:00', 0),
    (10, N'Investigate slow task list query',      N'List endpoint takes 1.8 s with 5k rows; check for a missing index.',     1, 2, N'Priya Nair',    '2026-09-25T16:00:00', '2026-09-28T11:40:00', 0),
    (11, N'Prepare sprint demo slides',            N'Five slides: shipped, in progress, blocked.',                           2, 0, N'Lena Fischer',  '2026-09-19T09:30:00', '2026-09-23T12:00:00', 0),
    (12, N'Rotate SQL Server service credentials', N'Quarterly rotation; update the Key Vault secret and restart the API.',  0, 3, N'Marta Silva',   '2026-09-21T09:15:00', '2026-09-27T16:40:00', 0),
    (13, N'Evaluate Trello export',                N'Superseded by this tool.',                                              0, 0, N'Sam Chen',      '2026-09-02T10:00:00', '2026-09-12T10:00:00', 1)
) AS source (Id, Title, Description, Status, Priority, AssignedTo, CreatedDate, ModifiedDate, IsDeleted)
ON target.Id = source.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Title, Description, Status, Priority, AssignedTo, CreatedDate, ModifiedDate, IsDeleted)
    VALUES (source.Id, source.Title, source.Description, source.Status, source.Priority, source.AssignedTo,
            source.CreatedDate, source.ModifiedDate, source.IsDeleted);

SET IDENTITY_INSERT dbo.Tasks OFF;
GO

SELECT COUNT(*) AS VisibleTasks FROM dbo.Tasks WHERE IsDeleted = 0; -- expect 12
GO
