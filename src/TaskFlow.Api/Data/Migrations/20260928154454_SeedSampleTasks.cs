using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedSampleTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "AssignedTo", "CreatedDate", "Description", "IsDeleted", "ModifiedDate", "Priority", "Status", "Title" },
                values: new object[,]
                {
                    { 1, "Priya Nair", new DateTime(2026, 9, 18, 10, 0, 0, 0, DateTimeKind.Utc), "Build, test and publish the API image on every pull request.", false, new DateTime(2026, 9, 28, 10, 0, 0, 0, DateTimeKind.Utc), (byte)2, (byte)1, "Set up CI pipeline for API" },
                    { 2, "Daniel Okafor", new DateTime(2026, 9, 15, 14, 20, 0, 0, DateTimeKind.Utc), "Move ReportRuns into the reporting schema; keep a view for old queries.", false, new DateTime(2026, 9, 26, 12, 0, 0, 0, DateTimeKind.Utc), (byte)3, (byte)0, "Migrate user reports to new schema" },
                    { 3, "Marta Silva", new DateTime(2026, 9, 24, 9, 5, 0, 0, DateTimeKind.Utc), "Due dates shift by one day for users in UTC−5 and further west.", false, new DateTime(2026, 9, 28, 11, 25, 0, 0, DateTimeKind.Utc), (byte)3, (byte)1, "Fix date picker timezone offset" },
                    { 4, "Sam Chen", new DateTime(2026, 9, 10, 11, 0, 0, 0, DateTimeKind.Utc), "Cover local setup, Docker, and how we triage tasks.", false, new DateTime(2026, 9, 25, 12, 0, 0, 0, DateTimeKind.Utc), (byte)0, (byte)0, "Write onboarding guide for new hires" },
                    { 5, "Aisha Rahman", new DateTime(2026, 9, 8, 8, 30, 0, 0, DateTimeKind.Utc), "Confirm leavers no longer have access to production resources.", false, new DateTime(2026, 9, 24, 12, 0, 0, 0, DateTimeKind.Utc), (byte)2, (byte)2, "Review Q3 access permissions audit" },
                    { 6, "Daniel Okafor", new DateTime(2026, 9, 22, 13, 10, 0, 0, DateTimeKind.Utc), "100 requests/min per key; return 429 with Retry-After.", false, new DateTime(2026, 9, 28, 7, 0, 0, 0, DateTimeKind.Utc), (byte)2, (byte)0, "Add rate limiting to public endpoints" },
                    { 7, "Sam Chen", new DateTime(2026, 9, 14, 15, 45, 0, 0, DateTimeKind.Utc), "Resolve the three high-severity advisories from npm audit.", false, new DateTime(2026, 9, 22, 12, 0, 0, 0, DateTimeKind.Utc), (byte)1, (byte)2, "Update npm dependencies after audit" },
                    { 8, "Lena Fischer", new DateTime(2026, 9, 21, 10, 30, 0, 0, DateTimeKind.Utc), "No tasks, no results and no-permission variants.", false, new DateTime(2026, 9, 28, 11, 0, 0, 0, DateTimeKind.Utc), (byte)1, (byte)1, "Design empty states for dashboard" },
                    { 9, "Aisha Rahman", new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Export to the records archive, then set the site to read-only.", false, new DateTime(2026, 9, 14, 12, 0, 0, 0, DateTimeKind.Utc), (byte)0, (byte)0, "Archive legacy SharePoint site" },
                    { 10, "Priya Nair", new DateTime(2026, 9, 25, 16, 0, 0, 0, DateTimeKind.Utc), "List endpoint takes 1.8 s with 5k rows; check for a missing index.", false, new DateTime(2026, 9, 28, 11, 40, 0, 0, DateTimeKind.Utc), (byte)2, (byte)1, "Investigate slow task list query" },
                    { 11, "Lena Fischer", new DateTime(2026, 9, 19, 9, 30, 0, 0, DateTimeKind.Utc), "Five slides: shipped, in progress, blocked.", false, new DateTime(2026, 9, 23, 12, 0, 0, 0, DateTimeKind.Utc), (byte)0, (byte)2, "Prepare sprint demo slides" },
                    { 12, "Marta Silva", new DateTime(2026, 9, 21, 9, 15, 0, 0, DateTimeKind.Utc), "Quarterly rotation; update the Key Vault secret and restart the API.", false, new DateTime(2026, 9, 27, 16, 40, 0, 0, DateTimeKind.Utc), (byte)3, (byte)0, "Rotate SQL Server service credentials" },
                    { 13, "Sam Chen", new DateTime(2026, 9, 2, 10, 0, 0, 0, DateTimeKind.Utc), "Superseded by this tool.", true, new DateTime(2026, 9, 12, 10, 0, 0, 0, DateTimeKind.Utc), (byte)0, (byte)0, "Evaluate Trello export" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 13);
        }
    }
}
