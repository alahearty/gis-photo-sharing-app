using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gis_photo_sharing_app.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoMetadataAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TakenAt",
                table: "Photos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Photos",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Photos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Photos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Photos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Photos\" SET \"Category\" = 'General' WHERE \"Category\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Category",
                table: "Photos",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Created",
                table: "Photos",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_CreatedByUserId",
                table: "Photos",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsPublic",
                table: "Photos",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Latitude_Longitude",
                table: "Photos",
                columns: new[] { "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_Category",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_Created",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_CreatedByUserId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsPublic",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_Latitude_Longitude",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Photos");

            migrationBuilder.AlterColumn<string>(
                name: "TakenAt",
                table: "Photos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Photos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
