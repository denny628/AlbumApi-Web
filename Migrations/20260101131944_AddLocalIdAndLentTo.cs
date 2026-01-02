using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlbumApi.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalIdAndLentTo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Albums",
                keyColumn: "Owner",
                keyValue: null,
                column: "Owner",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                table: "Albums",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LentTo",
                table: "Albums",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "LocalId",
                table: "Albums",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LentTo",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "LocalId",
                table: "Albums");

            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                table: "Albums",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
