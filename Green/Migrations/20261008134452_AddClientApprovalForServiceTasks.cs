using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Green.Migrations
{
    /// <inheritdoc />
    public partial class AddClientApprovalForServiceTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByClient",
                table: "ServiceTasks",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"ServiceTasks\" SET \"IsApprovedByClient\" = TRUE WHERE \"IsApprovedByClient\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApprovedByClient",
                table: "ServiceTasks");
        }
    }
}
