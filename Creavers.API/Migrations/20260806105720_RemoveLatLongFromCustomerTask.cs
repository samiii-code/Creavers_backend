using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creavers.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLatLongFromCustomerTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "CustomerTasks");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "CustomerTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "CustomerTasks",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "CustomerTasks",
                type: "double precision",
                nullable: true);
        }
    }
}
