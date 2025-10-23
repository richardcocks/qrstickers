using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class OverhaulDefaultsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateDeviceModels");

            migrationBuilder.DropTable(
                name: "TemplateDeviceTypes");

            migrationBuilder.DropIndex(
                name: "IX_StickerTemplates_IsDefault",
                table: "StickerTemplates");

            migrationBuilder.DropIndex(
                name: "IX_StickerTemplates_IsRackMount",
                table: "StickerTemplates");

            migrationBuilder.DropIndex(
                name: "IX_StickerTemplates_ProductTypeFilter",
                table: "StickerTemplates");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "StickerTemplates");

            migrationBuilder.DropColumn(
                name: "IsRackMount",
                table: "StickerTemplates");

            migrationBuilder.DropColumn(
                name: "ProductTypeFilter",
                table: "StickerTemplates");

            migrationBuilder.CreateTable(
                name: "ConnectionDefaultTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectionId = table.Column<int>(type: "int", nullable: false),
                    ProductType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionDefaultTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionDefaultTemplates_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConnectionDefaultTemplates_StickerTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "StickerTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionDefaultTemplates_ConnectionId",
                table: "ConnectionDefaultTemplates",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionDefaultTemplates_ConnectionId_ProductType",
                table: "ConnectionDefaultTemplates",
                columns: new[] { "ConnectionId", "ProductType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionDefaultTemplates_ProductType",
                table: "ConnectionDefaultTemplates",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionDefaultTemplates_TemplateId",
                table: "ConnectionDefaultTemplates",
                column: "TemplateId");

            // Seed ConnectionDefaultTemplates for existing connections
            migrationBuilder.Sql(@"
                -- Get template IDs
                DECLARE @RackTemplateId INT = (SELECT TOP 1 Id FROM StickerTemplates WHERE Name = 'Rack Mount Default' AND IsSystemTemplate = 1);
                DECLARE @CeilingTemplateId INT = (SELECT TOP 1 Id FROM StickerTemplates WHERE Name = 'Ceiling/Wall Mount Default' AND IsSystemTemplate = 1);

                -- Insert defaults for each existing connection
                INSERT INTO ConnectionDefaultTemplates (ConnectionId, ProductType, TemplateId, CreatedAt, UpdatedAt)
                SELECT
                    c.Id,
                    pt.ProductType,
                    CASE
                        WHEN pt.ProductType IN ('switch', 'appliance') THEN @RackTemplateId
                        ELSE @CeilingTemplateId
                    END,
                    GETUTCDATE(),
                    GETUTCDATE()
                FROM Connections c
                CROSS JOIN (
                    SELECT 'switch' AS ProductType
                    UNION SELECT 'appliance'
                    UNION SELECT 'wireless'
                    UNION SELECT 'camera'
                    UNION SELECT 'sensor'
                    UNION SELECT 'cellularGateway'
                ) pt
                WHERE NOT EXISTS (
                    SELECT 1 FROM ConnectionDefaultTemplates
                    WHERE ConnectionId = c.Id AND ProductType = pt.ProductType
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionDefaultTemplates");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "StickerTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRackMount",
                table: "StickerTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProductTypeFilter",
                table: "StickerTemplates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TemplateDeviceModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
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
                name: "IX_StickerTemplates_IsDefault",
                table: "StickerTemplates",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_StickerTemplates_IsRackMount",
                table: "StickerTemplates",
                column: "IsRackMount");

            migrationBuilder.CreateIndex(
                name: "IX_StickerTemplates_ProductTypeFilter",
                table: "StickerTemplates",
                column: "ProductTypeFilter");

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
    }
}
