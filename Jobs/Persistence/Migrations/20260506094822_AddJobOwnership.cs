using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobSchedulerPrototype.Jobs.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByActorId",
                table: "Jobs",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "dev-user");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Jobs",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "dev-tenant");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_TenantId",
                table: "Jobs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_TenantId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CreatedByActorId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Jobs");
        }
    }
}
