using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWD.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSensorData_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    OrgID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Organiza__420C9E0C845179E3", x => x.OrgID);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Role__8AFACE3A10B7CB9B", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "SensorType",
                columns: table => new
                {
                    TypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SensorTy__516F0395C1AE3D64", x => x.TypeID);
                });

            migrationBuilder.CreateTable(
                name: "SystemLog",
                columns: table => new
                {
                    LogID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "EOH-Webhook"),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SystemLo__5E5499A8547BC738", x => x.LogID);
                });

            migrationBuilder.CreateTable(
                name: "Site",
                columns: table => new
                {
                    SiteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GeoLocation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Site__B9DCB9032CBAC039", x => x.SiteID);
                    table.ForeignKey(
                        name: "FK__Site__OrgID__3A81B327",
                        column: x => x.OrgID,
                        principalTable: "Organization",
                        principalColumn: "OrgID");
                });

            migrationBuilder.CreateTable(
                name: "Hub",
                columns: table => new
                {
                    HubID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MacAddress = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    LastHandshake = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Hub__9F4FFECFC0EB50EB", x => x.HubID);
                    table.ForeignKey(
                        name: "FK__Hub__SiteID__3F466844",
                        column: x => x.SiteID,
                        principalTable: "Site",
                        principalColumn: "SiteID");
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrgID = table.Column<int>(type: "int", nullable: false),
                    SiteID = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__User__1788CCACE687B793", x => x.UserID);
                    table.ForeignKey(
                        name: "FK__User__OrgID__5165187F",
                        column: x => x.OrgID,
                        principalTable: "Organization",
                        principalColumn: "OrgID");
                    table.ForeignKey(
                        name: "FK__User__RoleId__534D60F1",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK__User__SiteID__52593CB8",
                        column: x => x.SiteID,
                        principalTable: "Site",
                        principalColumn: "SiteID");
                });

            migrationBuilder.CreateTable(
                name: "Sensor",
                columns: table => new
                {
                    SensorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HubID = table.Column<int>(type: "int", nullable: false),
                    TypeID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Sensor__D809841AFEF09FD0", x => x.SensorID);
                    table.ForeignKey(
                        name: "FK__Sensor__HubID__44FF419A",
                        column: x => x.HubID,
                        principalTable: "Hub",
                        principalColumn: "HubID");
                    table.ForeignKey(
                        name: "FK__Sensor__TypeID__45F365D3",
                        column: x => x.TypeID,
                        principalTable: "SensorType",
                        principalColumn: "TypeID");
                });

            migrationBuilder.CreateTable(
                name: "AlertRule",
                columns: table => new
                {
                    RuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SensorID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConditionType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    MinVal = table.Column<double>(type: "float", nullable: true),
                    MaxVal = table.Column<double>(type: "float", nullable: true),
                    NotificationMethod = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AlertRul__110458C20C3B9D6E", x => x.RuleID);
                    table.ForeignKey(
                        name: "FK__AlertRule__Senso__571DF1D5",
                        column: x => x.SensorID,
                        principalTable: "Sensor",
                        principalColumn: "SensorID");
                });

            migrationBuilder.CreateTable(
                name: "SensorData",
                columns: table => new
                {
                    DataID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SensorID = table.Column<int>(type: "int", nullable: false),
                    HubID = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SensorDa__C80F9C6E561FFF59", x => x.DataID);
                    table.ForeignKey(
                        name: "FK__SensorData__HubID__HubData",
                        column: x => x.HubID,
                        principalTable: "Hub",
                        principalColumn: "HubID");
                    table.ForeignKey(
                        name: "FK__SensorData__SensorI__49C3F6B7",
                        column: x => x.SensorID,
                        principalTable: "Sensor",
                        principalColumn: "SensorID");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    NotiID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    IsRead = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__EDC08EF20E2D3E48", x => x.NotiID);
                    table.ForeignKey(
                        name: "FK__Notificat__RuleID__60A75C0F",
                        column: x => x.RuleID,
                        principalTable: "AlertRule",
                        principalColumn: "RuleID");
                    table.ForeignKey(
                        name: "FK__Notificat__UserI__619B8048",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRule_SensorID",
                table: "AlertRule",
                column: "SensorID");

            migrationBuilder.CreateIndex(
                name: "IX_Hub_SiteID",
                table: "Hub",
                column: "SiteID");

            migrationBuilder.CreateIndex(
                name: "UQ__Hub__50EDF1CDFDF0BE28",
                table: "Hub",
                column: "MacAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_RuleID",
                table: "Notification",
                column: "RuleID");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserID",
                table: "Notification",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ__Role__8A2B6160395B11F5",
                table: "Role",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sensor_HubID",
                table: "Sensor",
                column: "HubID");

            migrationBuilder.CreateIndex(
                name: "IX_Sensor_TypeID",
                table: "Sensor",
                column: "TypeID");

            migrationBuilder.CreateIndex(
                name: "IDX_SensorData_Sensor_Time",
                table: "SensorData",
                columns: new[] { "SensorID", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_HubID",
                table: "SensorData",
                column: "HubID");

            migrationBuilder.CreateIndex(
                name: "IX_Site_OrgID",
                table: "Site",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_User_OrgID",
                table: "User",
                column: "OrgID");

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleId",
                table: "User",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_User_SiteID",
                table: "User",
                column: "SiteID");

            migrationBuilder.CreateIndex(
                name: "UQ__User__A9D105342B2B253F",
                table: "User",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "SensorData");

            migrationBuilder.DropTable(
                name: "SystemLog");

            migrationBuilder.DropTable(
                name: "AlertRule");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Sensor");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Hub");

            migrationBuilder.DropTable(
                name: "SensorType");

            migrationBuilder.DropTable(
                name: "Site");

            migrationBuilder.DropTable(
                name: "Organization");
        }
    }
}
