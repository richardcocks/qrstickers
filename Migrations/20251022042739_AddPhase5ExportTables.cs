using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase5ExportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    ConnectionId = table.Column<int>(type: "int", nullable: true),
                    ExportFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ExportDpi = table.Column<int>(type: "int", nullable: true),
                    BackgroundType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileSize = table.Column<int>(type: "int", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExportHistory_CachedDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "CachedDevices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExportHistory_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExportHistory_StickerTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "StickerTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateDeviceModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateDeviceModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateDeviceModels_StickerTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "StickerTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateDeviceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateDeviceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateDeviceTypes_StickerTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "StickerTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportHistory_ConnectionId",
                table: "ExportHistory",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportHistory_DeviceId",
                table: "ExportHistory",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportHistory_ExportedAt",
                table: "ExportHistory",
                column: "ExportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportHistory_TemplateId",
                table: "ExportHistory",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportHistory_UserId",
                table: "ExportHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateDeviceModels_DeviceModel",
                table: "TemplateDeviceModels",
                column: "DeviceModel");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateDeviceModels_TemplateId",
                table: "TemplateDeviceModels",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateDeviceModels_TemplateId_DeviceModel",
                table: "TemplateDeviceModels",
                columns: new[] { "TemplateId", "DeviceModel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateDeviceTypes_DeviceType",
                table: "TemplateDeviceTypes",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateDeviceTypes_TemplateId",
                table: "TemplateDeviceTypes",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateDeviceTypes_TemplateId_DeviceType",
                table: "TemplateDeviceTypes",
                columns: new[] { "TemplateId", "DeviceType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportHistory");

            migrationBuilder.DropTable(
                name: "TemplateDeviceModels");

            migrationBuilder.DropTable(
                name: "TemplateDeviceTypes");
        }
    }
}
