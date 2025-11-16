using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations.LogDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "log_entry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    backupPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    datetime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    filePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    size = table.Column<long>(type: "INTEGER", nullable: true),
                    action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_entry", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_log_entry_backupPlanId_datetime",
                table: "log_entry",
                columns: new[] { "backupPlanId", "datetime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_entry");
        }
    }
}
