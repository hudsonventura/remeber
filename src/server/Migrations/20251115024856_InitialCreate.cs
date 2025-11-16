using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    hostname = table.Column<string>(type: "TEXT", nullable: false),
                    token = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "backup_plan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    schedule = table.Column<string>(type: "TEXT", nullable: false),
                    source = table.Column<string>(type: "TEXT", nullable: false),
                    destination = table.Column<string>(type: "TEXT", nullable: false),
                    agentid = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_plan", x => x.id);
                    table.ForeignKey(
                        name: "FK_backup_plan_agent_agentid",
                        column: x => x.agentid,
                        principalTable: "agent",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_backup_plan_agentid",
                table: "backup_plan",
                column: "agentid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backup_plan");

            migrationBuilder.DropTable(
                name: "agent");
        }
    }
}
