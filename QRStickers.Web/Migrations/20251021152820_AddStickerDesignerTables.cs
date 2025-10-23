using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class AddStickerDesignerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyLogoUrl",
                table: "Connections",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GlobalVariables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectionId = table.Column<int>(type: "int", nullable: false),
                    VariableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VariableValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlobalVariables_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StickerTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectionId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProductTypeFilter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsRackMount = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false),
                    PageWidth = table.Column<double>(type: "float", nullable: false),
                    PageHeight = table.Column<double>(type: "float", nullable: false),
                    TemplateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StickerTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StickerTemplates_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalVariables_ConnectionId",
                table: "GlobalVariables",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalVariables_ConnectionId_VariableName",
                table: "GlobalVariables",
                columns: new[] { "ConnectionId", "VariableName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StickerTemplates_ConnectionId",
                table: "StickerTemplates",
                column: "ConnectionId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalVariables");

            migrationBuilder.DropTable(
                name: "StickerTemplates");

            migrationBuilder.DropColumn(
                name: "CompanyLogoUrl",
                table: "Connections");
        }
    }
}
