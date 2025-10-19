using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class UseCompositeKeysForCachedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CachedDevices_CachedNetworks_NetworkId",
                table: "CachedDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_CachedNetworks_CachedOrganizations_OrganizationId",
                table: "CachedNetworks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CachedOrganizations_OrganizationId",
                table: "CachedOrganizations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CachedNetworks_NetworkId",
                table: "CachedNetworks");

            migrationBuilder.DropIndex(
                name: "IX_CachedNetworks_OrganizationId",
                table: "CachedNetworks");

            migrationBuilder.DropIndex(
                name: "IX_CachedDevices_NetworkId",
                table: "CachedDevices");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CachedOrganizations_ConnectionId_OrganizationId",
                table: "CachedOrganizations",
                columns: new[] { "ConnectionId", "OrganizationId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CachedNetworks_ConnectionId_NetworkId",
                table: "CachedNetworks",
                columns: new[] { "ConnectionId", "NetworkId" });

            migrationBuilder.CreateIndex(
                name: "IX_CachedNetworks_ConnectionId_OrganizationId",
                table: "CachedNetworks",
                columns: new[] { "ConnectionId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_CachedDevices_ConnectionId_NetworkId",
                table: "CachedDevices",
                columns: new[] { "ConnectionId", "NetworkId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CachedDevices_CachedNetworks_ConnectionId_NetworkId",
                table: "CachedDevices",
                columns: new[] { "ConnectionId", "NetworkId" },
                principalTable: "CachedNetworks",
                principalColumns: new[] { "ConnectionId", "NetworkId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CachedNetworks_CachedOrganizations_ConnectionId_OrganizationId",
                table: "CachedNetworks",
                columns: new[] { "ConnectionId", "OrganizationId" },
                principalTable: "CachedOrganizations",
                principalColumns: new[] { "ConnectionId", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CachedDevices_CachedNetworks_ConnectionId_NetworkId",
                table: "CachedDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_CachedNetworks_CachedOrganizations_ConnectionId_OrganizationId",
                table: "CachedNetworks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CachedOrganizations_ConnectionId_OrganizationId",
                table: "CachedOrganizations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CachedNetworks_ConnectionId_NetworkId",
                table: "CachedNetworks");

            migrationBuilder.DropIndex(
                name: "IX_CachedNetworks_ConnectionId_OrganizationId",
                table: "CachedNetworks");

            migrationBuilder.DropIndex(
                name: "IX_CachedDevices_ConnectionId_NetworkId",
                table: "CachedDevices");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CachedOrganizations_OrganizationId",
                table: "CachedOrganizations",
                column: "OrganizationId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CachedNetworks_NetworkId",
                table: "CachedNetworks",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedNetworks_OrganizationId",
                table: "CachedNetworks",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedDevices_NetworkId",
                table: "CachedDevices",
                column: "NetworkId");

            migrationBuilder.AddForeignKey(
                name: "FK_CachedDevices_CachedNetworks_NetworkId",
                table: "CachedDevices",
                column: "NetworkId",
                principalTable: "CachedNetworks",
                principalColumn: "NetworkId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CachedNetworks_CachedOrganizations_OrganizationId",
                table: "CachedNetworks",
                column: "OrganizationId",
                principalTable: "CachedOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
