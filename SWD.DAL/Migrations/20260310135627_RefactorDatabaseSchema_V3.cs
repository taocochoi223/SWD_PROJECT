using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWD.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDatabaseSchema_V3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__AlertRule__Senso__571DF1D5",
                table: "AlertRule");

            migrationBuilder.DropForeignKey(
                name: "FK__SensorData__SensorI__49C3F6B7",
                table: "SensorData");

            migrationBuilder.DropIndex(
                name: "IDX_SensorData_Sensor_Time",
                table: "SensorData");

            migrationBuilder.DropIndex(
                name: "IX_SensorData_HubID",
                table: "SensorData");

            migrationBuilder.DropColumn(
                name: "SensorID",
                table: "SensorData");

            migrationBuilder.RenameColumn(
                name: "SensorID",
                table: "AlertRule",
                newName: "OrgID");

            migrationBuilder.RenameIndex(
                name: "IX_AlertRule_SensorID",
                table: "AlertRule",
                newName: "IX_AlertRule_OrgID");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "SensorData",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<int>(
                name: "OrgID",
                table: "Notification",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AlertRule",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HubID",
                table: "AlertRule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IDX_SensorData_Hub_Time",
                table: "SensorData",
                columns: new[] { "HubID", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_OrgID",
                table: "Notification",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRule_HubID",
                table: "AlertRule",
                column: "HubID");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertRule_Hub",
                table: "AlertRule",
                column: "HubID",
                principalTable: "Hub",
                principalColumn: "HubID");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertRule_Organization",
                table: "AlertRule",
                column: "OrgID",
                principalTable: "Organization",
                principalColumn: "OrgID");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_Organization",
                table: "Notification",
                column: "OrgID",
                principalTable: "Organization",
                principalColumn: "OrgID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlertRule_Hub",
                table: "AlertRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AlertRule_Organization",
                table: "AlertRule");

            migrationBuilder.DropForeignKey(
                name: "FK_Notification_Organization",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IDX_SensorData_Hub_Time",
                table: "SensorData");

            migrationBuilder.DropIndex(
                name: "IX_Notification_OrgID",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_AlertRule_HubID",
                table: "AlertRule");

            migrationBuilder.DropColumn(
                name: "OrgID",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "HubID",
                table: "AlertRule");

            migrationBuilder.RenameColumn(
                name: "OrgID",
                table: "AlertRule",
                newName: "SensorID");

            migrationBuilder.RenameIndex(
                name: "IX_AlertRule_OrgID",
                table: "AlertRule",
                newName: "IX_AlertRule_SensorID");

            migrationBuilder.AlterColumn<double>(
                name: "Value",
                table: "SensorData",
                type: "float",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "SensorID",
                table: "SensorData",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AlertRule",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IDX_SensorData_Sensor_Time",
                table: "SensorData",
                columns: new[] { "SensorID", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_HubID",
                table: "SensorData",
                column: "HubID");

            migrationBuilder.AddForeignKey(
                name: "FK__AlertRule__Senso__571DF1D5",
                table: "AlertRule",
                column: "SensorID",
                principalTable: "Sensor",
                principalColumn: "SensorID");

            migrationBuilder.AddForeignKey(
                name: "FK__SensorData__SensorI__49C3F6B7",
                table: "SensorData",
                column: "SensorID",
                principalTable: "Sensor",
                principalColumn: "SensorID");
        }
    }
}
