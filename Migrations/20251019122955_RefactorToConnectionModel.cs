using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToConnectionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CachedDevices_AspNetUsers_UserId",
                table: "CachedDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_CachedNetworks_AspNetUsers_UserId",
                table: "CachedNetworks");

            migrationBuilder.DropForeignKey(
                name: "FK_CachedOrganizations_AspNetUsers_UserId",
                table: "CachedOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncStatuses_AspNetUsers_UserId",
                table: "SyncStatuses");

            migrationBuilder.DropTable(
                name: "OAuthTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncStatuses",
                table: "SyncStatuses");

            migrationBuilder.DropIndex(
                name: "IX_CachedOrganizations_UserId",
                table: "CachedOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_CachedOrganizations_UserId_OrganizationId",
                table: "CachedOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_CachedNetworks_UserId",
                table: "CachedNetworks");

            migrationBuilder.DropIndex(
                name: "IX_CachedNetworks_UserId_NetworkId",
                table: "CachedNetworks");

            migrationBuilder.DropIndex(
                name: "IX_CachedDevices_UserId",
                table: "CachedDevices");

            migrationBuilder.DropIndex(
                name: "IX_CachedDevices_UserId_Serial",
                table: "CachedDevices");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SyncStatuses");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CachedOrganizations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CachedNetworks");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CachedDevices");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "SyncStatuses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "ConnectionId",
                table: "SyncStatuses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConnectionId",
                table: "CachedOrganizations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConnectionId",
                table: "CachedNetworks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConnectionId",
                table: "CachedDevices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncStatuses",
                table: "SyncStatuses",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ConnectionType = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Connections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MerakiOAuthTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConnectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerakiOAuthTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerakiOAuthTokens_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncStatuses_ConnectionId",
                table: "SyncStatuses",
                column: "ConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedOrganizations_ConnectionId",
                table: "CachedOrganizations",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedOrganizations_ConnectionId_OrganizationId",
                table: "CachedOrganizations",
                columns: new[] { "ConnectionId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedNetworks_ConnectionId",
                table: "CachedNetworks",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedNetworks_ConnectionId_NetworkId",
                table: "CachedNetworks",
                columns: new[] { "ConnectionId", "NetworkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedDevices_ConnectionId",
                table: "CachedDevices",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedDevices_ConnectionId_Serial",
                table: "CachedDevices",
                columns: new[] { "ConnectionId", "Serial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connections_UserId",
                table: "Connections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MerakiOAuthTokens_ConnectionId",
                table: "MerakiOAuthTokens",
                column: "ConnectionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CachedDevices_Connections_ConnectionId",
                table: "CachedDevices",
                column: "ConnectionId",
                principalTable: "Connections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CachedNetworks_Connections_ConnectionId",
                table: "CachedNetworks",
                column: "ConnectionId",
                principalTable: "Connections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CachedOrganizations_Connections_ConnectionId",
                table: "CachedOrganizations",
                column: "ConnectionId",
                principalTable: "Connections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncStatuses_Connections_ConnectionId",
                table: "SyncStatuses",
                column: "ConnectionId",
                principalTable: "Connections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CachedDevices_Connections_ConnectionId",
                table: "CachedDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_CachedNetworks_Connections_ConnectionId",
                table: "CachedNetworks");

            migrationBuilder.DropForeignKey(
                name: "FK_CachedOrganizations_Connections_ConnectionId",
                table: "CachedOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncStatuses_Connections_ConnectionId",
                table: "SyncStatuses");

            migrationBuilder.DropTable(
                name: "MerakiOAuthTokens");

            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncStatuses",
                table: "SyncStatuses");

            migrationBuilder.DropIndex(
                name: "IX_SyncStatuses_ConnectionId",
                table: "SyncStatuses");

            migrationBuilder.DropIndex(
                name: "IX_CachedOrganizations_ConnectionId",
                table: "CachedOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_CachedOrganizations_ConnectionId_OrganizationId",
                table: "CachedOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_CachedNetworks_ConnectionId",
                table: "CachedNetworks");

            migrationBuilder.DropIndex(
                name: "IX_CachedNetworks_ConnectionId_NetworkId",
                table: "CachedNetworks");

            migrationBuilder.DropIndex(
                name: "IX_CachedDevices_ConnectionId",
                table: "CachedDevices");

            migrationBuilder.DropIndex(
                name: "IX_CachedDevices_ConnectionId_Serial",
                table: "CachedDevices");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "SyncStatuses");

            migrationBuilder.DropColumn(
                name: "ConnectionId",
                table: "SyncStatuses");

            migrationBuilder.DropColumn(
                name: "ConnectionId",
                table: "CachedOrganizations");

            migrationBuilder.DropColumn(
                name: "ConnectionId",
                table: "CachedNetworks");

            migrationBuilder.DropColumn(
                name: "ConnectionId",
                table: "CachedDevices");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SyncStatuses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CachedOrganizations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CachedNetworks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CachedDevices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncStatuses",
                table: "SyncStatuses",
                column: "UserId");

            migrationBuilder.CreateTable(
                name: "OAuthTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedOrganizations_UserId",
                table: "CachedOrganizations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedOrganizations_UserId_OrganizationId",
                table: "CachedOrganizations",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedNetworks_UserId",
                table: "CachedNetworks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedNetworks_UserId_NetworkId",
                table: "CachedNetworks",
                columns: new[] { "UserId", "NetworkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedDevices_UserId",
                table: "CachedDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedDevices_UserId_Serial",
                table: "CachedDevices",
                columns: new[] { "UserId", "Serial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthTokens_UserId",
                table: "OAuthTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CachedDevices_AspNetUsers_UserId",
                table: "CachedDevices",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CachedNetworks_AspNetUsers_UserId",
                table: "CachedNetworks",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CachedOrganizations_AspNetUsers_UserId",
                table: "CachedOrganizations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncStatuses_AspNetUsers_UserId",
                table: "SyncStatuses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
