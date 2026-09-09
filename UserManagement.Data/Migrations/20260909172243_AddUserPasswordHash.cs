using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 9L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 10L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 11L,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");
        }
    }
}
