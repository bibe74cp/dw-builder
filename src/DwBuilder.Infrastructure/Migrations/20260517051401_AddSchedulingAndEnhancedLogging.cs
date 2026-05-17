using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DwBuilder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingAndEnhancedLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScheduleDaysOfWeek",
                schema: "_meta",
                table: "SourceTables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleDescription",
                schema: "_meta",
                table: "SourceTables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduleEnabled",
                schema: "_meta",
                table: "SourceTables",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
                name: "ScheduleType",
                schema: "_meta",
                table: "SourceTables",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorDetails",
                schema: "_meta",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExecutionDurationMs",
                schema: "_meta",
                table: "Logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobExecutionId",
                schema: "_meta",
                table: "Logs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobName",
                schema: "_meta",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageName",
                schema: "_meta",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowsDeleted",
                schema: "_meta",
                table: "Logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowsInserted",
                schema: "_meta",
                table: "Logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowsUpdated",
                schema: "_meta",
                table: "Logs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduleDaysOfWeek",
                schema: "_meta",
                table: "SourceTables");

            migrationBuilder.DropColumn(
                name: "ScheduleDescription",
                schema: "_meta",
                table: "SourceTables");

            migrationBuilder.DropColumn(
                name: "ScheduleEnabled",
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
                name: "ScheduleType",
                schema: "_meta",
                table: "SourceTables");

            migrationBuilder.DropColumn(
                name: "ErrorDetails",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "ExecutionDurationMs",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "JobExecutionId",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "JobName",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "PackageName",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RowsDeleted",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RowsInserted",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RowsUpdated",
                schema: "_meta",
                table: "Logs");
        }
    }
}
