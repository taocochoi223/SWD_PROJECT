using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWD.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameValueToJsonValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "SensorData",
                newName: "JsonValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JsonValue",
                table: "SensorData",
                newName: "Value");
        }
    }
}
