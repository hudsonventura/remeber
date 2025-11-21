using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations.LogDb
{
    /// <inheritdoc />
    public partial class AddBackupExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create backup_execution table
            migrationBuilder.CreateTable(
                name: "backup_execution",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    backupPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    startDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    endDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_execution", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_backup_execution_backupPlanId_startDateTime",
                table: "backup_execution",
                columns: new[] { "backupPlanId", "startDateTime" });

            // Add executionId column to log_entry
            // For SQLite, we need to handle this carefully since we can't add NOT NULL columns directly
            // We'll add it as nullable first, then update existing rows, then make it required
            migrationBuilder.AddColumn<Guid>(
                name: "executionId",
                table: "log_entry",
                type: "TEXT",
                nullable: true);

            // Create a temporary execution for existing logs (if any)
            // This ensures existing logs have a valid executionId
            // Note: We'll create executions using a simple approach - one per backupPlanId
            migrationBuilder.Sql(@"
                INSERT INTO backup_execution (id, backupPlanId, name, startDateTime, endDateTime)
                SELECT 
                    lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6))),
                    backupPlanId,
                    'Migration execution',
                    MIN(datetime),
                    MAX(datetime)
                FROM log_entry
                GROUP BY backupPlanId
                HAVING COUNT(*) > 0;
            ");

            // Update existing log entries to use the migration execution
            migrationBuilder.Sql(@"
                UPDATE log_entry
                SET executionId = (
                    SELECT id 
                    FROM backup_execution 
                    WHERE backup_execution.backupPlanId = log_entry.backupPlanId 
                    LIMIT 1
                )
                WHERE executionId IS NULL;
            ");

            // Now make executionId required (SQLite doesn't support ALTER COLUMN, so we recreate the table)
            // This is a complex operation, so we'll use a workaround
            migrationBuilder.Sql(@"
                CREATE TABLE log_entry_new (
                    id TEXT NOT NULL PRIMARY KEY,
                    backupPlanId TEXT NOT NULL,
                    executionId TEXT NOT NULL,
                    datetime TEXT NOT NULL,
                    fileName TEXT NOT NULL,
                    filePath TEXT NOT NULL,
                    size INTEGER,
                    action TEXT NOT NULL,
                    reason TEXT NOT NULL
                );

                INSERT INTO log_entry_new 
                SELECT id, backupPlanId, executionId, datetime, fileName, filePath, size, action, reason
                FROM log_entry;

                DROP TABLE log_entry;
                ALTER TABLE log_entry_new RENAME TO log_entry;
            ");

            // Recreate indexes
            migrationBuilder.CreateIndex(
                name: "IX_log_entry_backupPlanId_datetime",
                table: "log_entry",
                columns: new[] { "backupPlanId", "datetime" });

            migrationBuilder.CreateIndex(
                name: "IX_log_entry_executionId",
                table: "log_entry",
                column: "executionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_log_entry_executionId",
                table: "log_entry");

            migrationBuilder.DropIndex(
                name: "IX_backup_execution_backupPlanId_startDateTime",
                table: "backup_execution");

            migrationBuilder.DropTable(
                name: "backup_execution");

            // Remove executionId column (SQLite workaround)
            migrationBuilder.Sql(@"
                CREATE TABLE log_entry_new (
                    id TEXT NOT NULL PRIMARY KEY,
                    backupPlanId TEXT NOT NULL,
                    datetime TEXT NOT NULL,
                    fileName TEXT NOT NULL,
                    filePath TEXT NOT NULL,
                    size INTEGER,
                    action TEXT NOT NULL,
                    reason TEXT NOT NULL
                );

                INSERT INTO log_entry_new 
                SELECT id, backupPlanId, datetime, fileName, filePath, size, action, reason
                FROM log_entry;

                DROP TABLE log_entry;
                ALTER TABLE log_entry_new RENAME TO log_entry;
            ");

            // Recreate original index
            migrationBuilder.CreateIndex(
                name: "IX_log_entry_backupPlanId_datetime",
                table: "log_entry",
                columns: new[] { "backupPlanId", "datetime" });
        }
    }
}

