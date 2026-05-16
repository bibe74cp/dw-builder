using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DwBuilder.Infrastructure.Migrations
{
    /// <summary>
    /// FASE 7: Adds scheduling configuration columns to SourceTables for SQL Server Agent integration.
    /// Author: db-developer
    /// Date: 2026-05-16
    /// </summary>
    public partial class AddSchedulingToSourceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ScheduleEnabled",
                schema: "_meta",
                table: "SourceTables",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleType",
                schema: "_meta",
                table: "SourceTables",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleFrequency",
                schema: "_meta",
                table: "SourceTables",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduleTime",
                schema: "_meta",
                table: "SourceTables",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleDaysOfWeek",
                schema: "_meta",
                table: "SourceTables",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleDescription",
                schema: "_meta",
                table: "SourceTables",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SourceTables_ScheduleType",
                schema: "_meta",
                table: "SourceTables",
                sql: "ScheduleType IN ('Daily', 'Weekly', 'Monthly', 'OnDemand') OR ScheduleType IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SourceTables_ScheduleType",
                schema: "_meta",
                table: "SourceTables");

            migrationBuilder.DropColumn(
                name: "ScheduleEnabled",
                schema: "_meta",
                table: "SourceTables");

            migrationBuilder.DropColumn(
                name: "ScheduleType",
                schema: "_meta",
                table: "SourceTables");

            migrationBuilder.DropColumn(
                name: "ScheduleFrequency",
                schema: "_meta",
                table: "SourceTables");

            migrationBuilder.DropColumn(
                name: "ScheduleTime",
                schema: "_meta",
                table: "SourceTables");

            migrationBuilder.DropColumn(
                name: "ScheduleDaysOfWeek",
                schema: "_meta",
                table: "SourceTables");

            migrationBuilder.DropColumn(
                name: "ScheduleDescription",
                schema: "_meta",
                table: "SourceTables");
        }
    }
}
