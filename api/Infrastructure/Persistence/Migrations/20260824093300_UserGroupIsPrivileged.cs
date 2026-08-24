using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserGroupIsPrivileged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivileged",
                table: "UserGroups",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Privileged bootstrap groups (global admin). Membership and high-risk permissions are restricted.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrivileged",
                table: "UserGroups");
        }
    }
}
