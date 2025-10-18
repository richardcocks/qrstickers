using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRStickers.Migrations
{
    /// <inheritdoc />
    public partial class RefactorOAuthToRefreshTokenOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessToken",
                table: "OAuthTokens");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "OAuthTokens",
                newName: "RefreshTokenExpiresAt");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "OAuthTokens",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefreshTokenExpiresAt",
                table: "OAuthTokens",
                newName: "ExpiresAt");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "OAuthTokens",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "AccessToken",
                table: "OAuthTokens",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
