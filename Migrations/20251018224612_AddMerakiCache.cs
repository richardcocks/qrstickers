using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class AddMerakiCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedOrganizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedOrganizations", x => x.Id);
                    table.UniqueConstraint("AK_CachedOrganizations_OrganizationId", x => x.OrganizationId);
                    table.ForeignKey(
                        name: "FK_CachedOrganizations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncStatuses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LastSyncStartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncCompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStep = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentStepNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSteps = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStatuses", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_SyncStatuses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CachedNetworks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                    NetworkId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    ProductTypesJson = table.Column<string>(type: "TEXT", nullable: true),
                    TagsJson = table.Column<string>(type: "TEXT", nullable: true),
                    TimeZone = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedNetworks", x => x.Id);
                    table.UniqueConstraint("AK_CachedNetworks_NetworkId", x => x.NetworkId);
                    table.ForeignKey(
                        name: "FK_CachedNetworks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CachedNetworks_CachedOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "CachedOrganizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CachedDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    NetworkId = table.Column<string>(type: "TEXT", nullable: true),
                    Serial = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CachedDevices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CachedDevices_CachedNetworks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "CachedNetworks",
                        principalColumn: "NetworkId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedDevices_NetworkId",
                table: "CachedDevices",
                column: "NetworkId");

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
                name: "IX_CachedNetworks_OrganizationId",
                table: "CachedNetworks",
                column: "OrganizationId");

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
                name: "IX_CachedOrganizations_UserId",
                table: "CachedOrganizations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedOrganizations_UserId_OrganizationId",
                table: "CachedOrganizations",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedDevices");

            migrationBuilder.DropTable(
                name: "SyncStatuses");

            migrationBuilder.DropTable(
                name: "CachedNetworks");

            migrationBuilder.DropTable(
                name: "CachedOrganizations");
        }
    }
}
