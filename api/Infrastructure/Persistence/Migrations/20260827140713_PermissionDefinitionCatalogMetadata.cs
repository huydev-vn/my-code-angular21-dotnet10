using Microsoft.EntityFrameworkCore.Migrations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260827140713_PermissionDefinitionCatalogMetadata")]
    public partial class PermissionDefinitionCatalogMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemManaged",
                table: "PermissionDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "True for seeded system permissions; Code remains immutable either way.");

            migrationBuilder.AddColumn<string>(
                name: "Resource",
                table: "PermissionDefinitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                comment: "Stable resource key for future scope enforcement (e.g. users, authorization.permissions).");

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "PermissionDefinitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Medium",
                comment: "Low | Medium | High | Critical — Critical is privileged-assignable-only.");

            migrationBuilder.AddColumn<string>(
                name: "ScopeMode",
                table: "PermissionDefinitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Global",
                comment: "None | OrganizationUnit | Global — catalog metadata for scope enforcement.");

            migrationBuilder.AlterColumn<string>(
                name: "Module",
                table: "PermissionDefinitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                comment: "Display/grouping module; Resource is the stable enforcement key.",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            // Backfill existing rows from Module / code heuristics.
            migrationBuilder.Sql(
                """
                UPDATE "PermissionDefinitions"
                SET "Resource" = "Module"
                WHERE "Resource" IS NULL AND "Module" IS NOT NULL;

                UPDATE "PermissionDefinitions"
                SET "Resource" = 'authorization.permissions'
                WHERE "Code" LIKE 'authorization.permissions.%';

                UPDATE "PermissionDefinitions"
                SET "Resource" = 'authorization.groups'
                WHERE "Code" LIKE 'authorization.groups.%';

                UPDATE "PermissionDefinitions"
                SET "Resource" = 'authorization.organization-units'
                WHERE "Code" LIKE 'authorization.organization-units.%';

                UPDATE "PermissionDefinitions"
                SET "Resource" = 'authorization.audit'
                WHERE "Code" LIKE 'authorization.audit.%';

                UPDATE "PermissionDefinitions"
                SET "ScopeMode" = 'Global'
                WHERE "Code" LIKE 'authorization.%' OR "Code" LIKE 'users.%';

                UPDATE "PermissionDefinitions"
                SET "RiskLevel" = 'Critical'
                WHERE "Code" IN (
                    'authorization.permissions.write',
                    'authorization.groups.write',
                    'authorization.organization-units.write');

                UPDATE "PermissionDefinitions"
                SET "RiskLevel" = 'High'
                WHERE "Code" = 'users.write';

                UPDATE "PermissionDefinitions"
                SET "RiskLevel" = 'Medium'
                WHERE "RiskLevel" IS NULL OR "RiskLevel" = '';

                UPDATE "PermissionDefinitions"
                SET "IsSystemManaged" = TRUE
                WHERE "Code" IN (
                    'users.read',
                    'users.write',
                    'authorization.permissions.read',
                    'authorization.permissions.write',
                    'authorization.groups.read',
                    'authorization.groups.write',
                    'authorization.organization-units.read',
                    'authorization.organization-units.write',
                    'authorization.audit.read');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystemManaged",
                table: "PermissionDefinitions");

            migrationBuilder.DropColumn(
                name: "Resource",
                table: "PermissionDefinitions");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "PermissionDefinitions");

            migrationBuilder.DropColumn(
                name: "ScopeMode",
                table: "PermissionDefinitions");

            migrationBuilder.AlterColumn<string>(
                name: "Module",
                table: "PermissionDefinitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "Display/grouping module; Resource is the stable enforcement key.");
        }
    }
}
