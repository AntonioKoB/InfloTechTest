using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UserManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Forename = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "UserLogs",
                columns: new[] { "Id", "Action", "AfterJson", "BeforeJson", "Timestamp", "UserId" },
                values: new object[,]
                {
                    { 1L, 0, "{\"Id\":1,\"Forename\":\"Peter\",\"Surname\":\"Loew\",\"Email\":\"ploew@example.com\",\"IsActive\":true,\"DateOfBirth\":\"1955-03-22\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 66, DateTimeKind.Utc).AddTicks(3871), 1L },
                    { 2L, 0, "{\"Id\":2,\"Forename\":\"Benjamin Franklin\",\"Surname\":\"Gates\",\"Email\":\"bfgates@example.com\",\"IsActive\":true,\"DateOfBirth\":\"1968-07-15\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(6984), 2L },
                    { 3L, 0, "{\"Id\":3,\"Forename\":\"Castor\",\"Surname\":\"Troy\",\"Email\":\"ctroy@example.com\",\"IsActive\":false,\"DateOfBirth\":\"1970-11-02\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(7504), 3L },
                    { 4L, 0, "{\"Id\":4,\"Forename\":\"Memphis\",\"Surname\":\"Raines\",\"Email\":\"mraines@example.com\",\"IsActive\":true,\"DateOfBirth\":\"1965-05-30\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(7544), 4L },
                    { 5L, 0, "{\"Id\":5,\"Forename\":\"Stanley\",\"Surname\":\"Goodspeed\",\"Email\":\"sgodspeed@example.com\",\"IsActive\":true,\"DateOfBirth\":\"1972-09-18\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(7560), 5L },
                    { 6L, 0, "{\"Id\":6,\"Forename\":\"H.I.\",\"Surname\":\"McDunnough\",\"Email\":\"himcdunnough@example.com\",\"IsActive\":true,\"DateOfBirth\":\"1958-02-10\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(7622), 6L },
                    { 7L, 0, "{\"Id\":7,\"Forename\":\"Cameron\",\"Surname\":\"Poe\",\"Email\":\"cpoe@example.com\",\"IsActive\":false,\"DateOfBirth\":\"1975-04-05\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(7637), 7L },
                    { 8L, 0, "{\"Id\":8,\"Forename\":\"Edward\",\"Surname\":\"Malus\",\"Email\":\"emalus@example.com\",\"IsActive\":false,\"DateOfBirth\":\"1969-12-25\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(7647), 8L },
                    { 9L, 0, "{\"Id\":9,\"Forename\":\"Damon\",\"Surname\":\"Macready\",\"Email\":\"dmacready@example.com\",\"IsActive\":false,\"DateOfBirth\":\"1960-08-14\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(7658), 9L },
                    { 10L, 0, "{\"Id\":10,\"Forename\":\"Johnny\",\"Surname\":\"Blaze\",\"Email\":\"jblaze@example.com\",\"IsActive\":true,\"DateOfBirth\":\"1980-06-21\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(7676), 10L },
                    { 11L, 0, "{\"Id\":11,\"Forename\":\"Robin\",\"Surname\":\"Feld\",\"Email\":\"rfeld@example.com\",\"IsActive\":true,\"DateOfBirth\":\"1963-01-09\"}", null, new DateTime(2026, 9, 8, 15, 14, 15, 84, DateTimeKind.Utc).AddTicks(7687), 11L }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DateOfBirth", "Email", "Forename", "IsActive", "Surname" },
                values: new object[,]
                {
                    { 1L, new DateOnly(1955, 3, 22), "ploew@example.com", "Peter", true, "Loew" },
                    { 2L, new DateOnly(1968, 7, 15), "bfgates@example.com", "Benjamin Franklin", true, "Gates" },
                    { 3L, new DateOnly(1970, 11, 2), "ctroy@example.com", "Castor", false, "Troy" },
                    { 4L, new DateOnly(1965, 5, 30), "mraines@example.com", "Memphis", true, "Raines" },
                    { 5L, new DateOnly(1972, 9, 18), "sgodspeed@example.com", "Stanley", true, "Goodspeed" },
                    { 6L, new DateOnly(1958, 2, 10), "himcdunnough@example.com", "H.I.", true, "McDunnough" },
                    { 7L, new DateOnly(1975, 4, 5), "cpoe@example.com", "Cameron", false, "Poe" },
                    { 8L, new DateOnly(1969, 12, 25), "emalus@example.com", "Edward", false, "Malus" },
                    { 9L, new DateOnly(1960, 8, 14), "dmacready@example.com", "Damon", false, "Macready" },
                    { 10L, new DateOnly(1980, 6, 21), "jblaze@example.com", "Johnny", true, "Blaze" },
                    { 11L, new DateOnly(1963, 1, 9), "rfeld@example.com", "Robin", true, "Feld" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLogs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
