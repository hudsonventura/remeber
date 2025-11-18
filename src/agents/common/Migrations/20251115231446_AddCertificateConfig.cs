using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentCommon.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if table exists before creating it (handles case where table was created manually)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS certificate_config (
                    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    certificatePath TEXT NOT NULL,
                    certificatePassword TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certificate_config");
        }
    }
}
