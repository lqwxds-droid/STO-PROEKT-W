using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Green.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedDateToServiceTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTask_ServiceRequests_ServiceRequestId",
                table: "ServiceTask");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServiceTask",
                table: "ServiceTask");

            migrationBuilder.RenameTable(
                name: "ServiceTask",
                newName: "ServiceTasks");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceTask_ServiceRequestId",
                table: "ServiceTasks",
                newName: "IX_ServiceTasks_ServiceRequestId");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedDate",
                table: "ServiceTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServiceTasks",
                table: "ServiceTasks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTasks_ServiceRequests_ServiceRequestId",
                table: "ServiceTasks",
                column: "ServiceRequestId",
                principalTable: "ServiceRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTasks_ServiceRequests_ServiceRequestId",
                table: "ServiceTasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServiceTasks",
                table: "ServiceTasks");

            migrationBuilder.DropColumn(
                name: "PlannedDate",
                table: "ServiceTasks");

            migrationBuilder.RenameTable(
                name: "ServiceTasks",
                newName: "ServiceTask");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceTasks_ServiceRequestId",
                table: "ServiceTask",
                newName: "IX_ServiceTask_ServiceRequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServiceTask",
                table: "ServiceTask",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTask_ServiceRequests_ServiceRequestId",
                table: "ServiceTask",
                column: "ServiceRequestId",
                principalTable: "ServiceRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
