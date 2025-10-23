using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class RemoveImageNameUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UploadedImages_ConnectionId_Name",
                table: "UploadedImages");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedImages_ConnectionId_Name",
                table: "UploadedImages",
                columns: new[] { "ConnectionId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UploadedImages_ConnectionId_Name",
                table: "UploadedImages");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedImages_ConnectionId_Name",
                table: "UploadedImages",
                columns: new[] { "ConnectionId", "Name" },
                unique: true);
        }
    }
}
