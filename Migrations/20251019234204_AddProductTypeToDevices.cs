using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypeToDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "CachedDevices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "CachedDevices");
        }
    }
}
