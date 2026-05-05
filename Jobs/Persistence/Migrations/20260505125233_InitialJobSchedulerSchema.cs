using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobSchedulerPrototype.Jobs.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialJobSchedulerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CurrentStateChangeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunAt = table.Column<long>(type: "INTEGER", nullable: true),
                    MaxAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobStateChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStateChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobStateChanges_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status_RunAt",
                table: "Jobs",
                columns: new[] { "Status", "RunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobStateChanges_JobId_Sequence",
                table: "JobStateChanges",
                columns: new[] { "JobId", "Sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobStateChanges");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
