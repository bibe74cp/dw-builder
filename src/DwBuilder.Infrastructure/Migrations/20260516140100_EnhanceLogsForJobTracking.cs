using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DwBuilder.Infrastructure.Migrations
{
    /// <summary>
    /// FASE 7: Enhances Logs table with SQL Server Agent job execution tracking columns.
    /// Author: db-developer
    /// Date: 2026-05-16
    /// </summary>
    public partial class EnhanceLogsForJobTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobName",
                schema: "_meta",
                table: "Logs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobExecutionId",
                schema: "_meta",
                table: "Logs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageName",
                schema: "_meta",
                table: "Logs",
                type: "nvarchar(200)",
                maxLength: 200,
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

            migrationBuilder.AddColumn<int>(
                name: "RowsDeleted",
                schema: "_meta",
                table: "Logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExecutionDurationMs",
                schema: "_meta",
                table: "Logs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorDetails",
                schema: "_meta",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: true);

            // Create index on JobName for faster job execution history queries
            migrationBuilder.CreateIndex(
                name: "IX_Logs_JobName",
                schema: "_meta",
                table: "Logs",
                column: "JobName");

            // Create index on JobExecutionId for correlation with SQL Agent execution
            migrationBuilder.CreateIndex(
                name: "IX_Logs_JobExecutionId",
                schema: "_meta",
                table: "Logs",
                column: "JobExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Logs_JobName",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropIndex(
                name: "IX_Logs_JobExecutionId",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "JobName",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "JobExecutionId",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "PackageName",
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

            migrationBuilder.DropColumn(
                name: "RowsDeleted",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "ExecutionDurationMs",
                schema: "_meta",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "ErrorDetails",
                schema: "_meta",
                table: "Logs");
        }
    }
}
