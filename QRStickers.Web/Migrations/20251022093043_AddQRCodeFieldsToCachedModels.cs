using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class AddQRCodeFieldsToCachedModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QRCodeDataUri",
                table: "CachedOrganizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QRCodeDataUri",
                table: "CachedNetworks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QRCodeDataUri",
                table: "CachedDevices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QRCodeDataUri",
                table: "CachedOrganizations");

            migrationBuilder.DropColumn(
                name: "QRCodeDataUri",
                table: "CachedNetworks");

            migrationBuilder.DropColumn(
                name: "QRCodeDataUri",
                table: "CachedDevices");
        }
    }
}
