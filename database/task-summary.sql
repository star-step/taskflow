/*
  Task counts by status and priority, as served by GET /api/tasks/summary.
  The API runs this exact query through ADO.NET (SqlTaskSummaryQuery), not EF Core.

  One GROUPING SETS query returns four kinds of row:
    IsStatusRollup = 0, IsPriorityRollup = 0  → one status × priority cell
    IsStatusRollup = 0, IsPriorityRollup = 1  → total for a status
    IsStatusRollup = 1, IsPriorityRollup = 0  → total for a priority
    IsStatusRollup = 1, IsPriorityRollup = 1  → grand total

  Status:   0 = To Do, 1 = In Progress, 2 = Done
  Priority: 0 = Low, 1 = Medium, 2 = High, 3 = Critical

  EF's soft-delete query filter does not apply to raw SQL, hence the explicit IsDeleted = 0.
*/
USE TaskFlow;
GO

SELECT
    t.[Status],
    t.[Priority],
    COUNT(*)               AS TaskCount,
    GROUPING(t.[Status])   AS IsStatusRollup,
    GROUPING(t.[Priority]) AS IsPriorityRollup
FROM dbo.Tasks AS t
WHERE t.IsDeleted = 0
GROUP BY GROUPING SETS (
    (t.[Status], t.[Priority]),
    (t.[Status]),
    (t.[Priority]),
    ()
)
ORDER BY IsStatusRollup, IsPriorityRollup, t.[Status], t.[Priority];
GO

/* Readable version for people: names instead of codes, one row per status/priority pair. */
SELECT
    CASE t.[Status] WHEN 0 THEN 'To Do' WHEN 1 THEN 'In Progress' WHEN 2 THEN 'Done' END AS [Status],
    CASE t.[Priority] WHEN 0 THEN 'Low' WHEN 1 THEN 'Medium' WHEN 2 THEN 'High' WHEN 3 THEN 'Critical' END AS [Priority],
    COUNT(*) AS TaskCount
FROM dbo.Tasks AS t
WHERE t.IsDeleted = 0
GROUP BY t.[Status], t.[Priority]
ORDER BY t.[Status], t.[Priority] DESC;
GO
