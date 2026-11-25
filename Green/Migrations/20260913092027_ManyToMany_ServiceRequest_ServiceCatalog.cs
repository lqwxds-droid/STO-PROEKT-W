using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Green.Migrations
{
    /// <inheritdoc />
    public partial class ManyToMany_ServiceRequest_ServiceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceCatalogServiceRequest");

            migrationBuilder.CreateTable(
                name: "ServiceRequestServiceCatalogs",
                columns: table => new
                {
                    ServiceRequestId = table.Column<int>(type: "integer", nullable: false),
                    ServiceCatalogId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequestServiceCatalogs", x => new { x.ServiceRequestId, x.ServiceCatalogId });
                    table.ForeignKey(
                        name: "FK_ServiceRequestServiceCatalogs_ServiceCatalogs_ServiceCatalo~",
                        column: x => x.ServiceCatalogId,
                        principalTable: "ServiceCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceRequestServiceCatalogs_ServiceRequests_ServiceReques~",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestServiceCatalogs_ServiceCatalogId",
                table: "ServiceRequestServiceCatalogs",
                column: "ServiceCatalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceRequestServiceCatalogs");

            migrationBuilder.CreateTable(
                name: "ServiceCatalogServiceRequest",
                columns: table => new
                {
                    SelectedServicesId = table.Column<int>(type: "integer", nullable: false),
                    ServiceRequestsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCatalogServiceRequest", x => new { x.SelectedServicesId, x.ServiceRequestsId });
                    table.ForeignKey(
                        name: "FK_ServiceCatalogServiceRequest_ServiceCatalogs_SelectedServic~",
                        column: x => x.SelectedServicesId,
                        principalTable: "ServiceCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceCatalogServiceRequest_ServiceRequests_ServiceRequest~",
                        column: x => x.ServiceRequestsId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCatalogServiceRequest_ServiceRequestsId",
                table: "ServiceCatalogServiceRequest",
                column: "ServiceRequestsId");
        }
    }
}
