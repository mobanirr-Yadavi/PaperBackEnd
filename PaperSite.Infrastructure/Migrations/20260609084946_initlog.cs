using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PaperSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initlog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    controllerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    requestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    responseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    persianDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Time = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    statusCode = table.Column<int>(type: "int", nullable: false),
                    ipAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    userId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
