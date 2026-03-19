using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWD.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAlertRuleWithType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TypeID",
                table: "AlertRule",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertRule_TypeID",
                table: "AlertRule",
                column: "TypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertRule_SensorType",
                table: "AlertRule",
                column: "TypeID",
                principalTable: "SensorType",
                principalColumn: "TypeID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlertRule_SensorType",
                table: "AlertRule");

            migrationBuilder.DropIndex(
                name: "IX_AlertRule_TypeID",
                table: "AlertRule");

            migrationBuilder.DropColumn(
                name: "TypeID",
                table: "AlertRule");
        }
    }
}
