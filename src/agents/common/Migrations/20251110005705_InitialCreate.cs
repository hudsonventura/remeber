using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentCommon.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if tables exist before creating them (handles case where tables were created manually)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS agent_token (
                    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    token TEXT NOT NULL,
                    created_at TEXT NOT NULL
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS pairing_code (
                    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    code TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    expires_at TEXT NOT NULL
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_token");

            migrationBuilder.DropTable(
                name: "pairing_code");
        }
    }
}
