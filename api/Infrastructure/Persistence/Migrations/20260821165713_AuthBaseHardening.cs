using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthBaseHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "UserGroups",
                comment: "Nhóm phân quyền nghiệp vụ: tập hợp permission chức năng và phạm vi đơn vị.");

            migrationBuilder.AlterTable(
                name: "UserGroupMemberships",
                comment: "Bảng nối user ↔ nhóm phân quyền: user thuộc group nào.");

            migrationBuilder.AlterTable(
                name: "RefreshTokens",
                comment: "Lịch sử refresh token (chỉ lưu hash). Mỗi login/refresh tạo bản ghi mới; token cũ bị revoke nhưng giữ lại để phát hiện tái sử dụng (replay).");

            migrationBuilder.AlterTable(
                name: "PermissionDefinitions",
                comment: "Danh mục quyền chức năng (permission catalog), ví dụ users.read. Không phải nhóm user.");

            migrationBuilder.AlterTable(
                name: "OrganizationUnits",
                comment: "Cây đơn vị tổ chức/phòng ban — phạm vi dữ liệu cha-con.");

            migrationBuilder.AlterTable(
                name: "GroupPermissions",
                comment: "Bảng nối nhóm ↔ quyền chức năng: group được phép làm gì.");

            migrationBuilder.AlterTable(
                name: "GroupOrganizationUnits",
                comment: "Bảng nối nhóm ↔ đơn vị: phạm vi dữ liệu của group (đơn vị và các con).");

            migrationBuilder.AlterTable(
                name: "AuthorizationAuditEvents",
                comment: "Nhật ký thay đổi quản trị authorization (actor, hành động, entity, trace).");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "Hash SHA-256 của refresh token; không lưu plaintext.",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Thời điểm revoke; null = còn hiệu lực (nếu chưa hết hạn).",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FamilyId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                comment: "Nhóm token cùng phiên đăng nhập; revoke cả family khi phát hiện replay.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Thời điểm hết hạn; dùng cho cleanup retention.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "PermissionDefinitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                comment: "Mã quyền ổn định dùng trong policy.",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_RevokedAt",
                table: "RefreshTokens",
                column: "RevokedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_RevokedAt",
                table: "RefreshTokens");

            migrationBuilder.AlterTable(
                name: "UserGroups",
                oldComment: "Nhóm phân quyền nghiệp vụ: tập hợp permission chức năng và phạm vi đơn vị.");

            migrationBuilder.AlterTable(
                name: "UserGroupMemberships",
                oldComment: "Bảng nối user ↔ nhóm phân quyền: user thuộc group nào.");

            migrationBuilder.AlterTable(
                name: "RefreshTokens",
                oldComment: "Lịch sử refresh token (chỉ lưu hash). Mỗi login/refresh tạo bản ghi mới; token cũ bị revoke nhưng giữ lại để phát hiện tái sử dụng (replay).");

            migrationBuilder.AlterTable(
                name: "PermissionDefinitions",
                oldComment: "Danh mục quyền chức năng (permission catalog), ví dụ users.read. Không phải nhóm user.");

            migrationBuilder.AlterTable(
                name: "OrganizationUnits",
                oldComment: "Cây đơn vị tổ chức/phòng ban — phạm vi dữ liệu cha-con.");

            migrationBuilder.AlterTable(
                name: "GroupPermissions",
                oldComment: "Bảng nối nhóm ↔ quyền chức năng: group được phép làm gì.");

            migrationBuilder.AlterTable(
                name: "GroupOrganizationUnits",
                oldComment: "Bảng nối nhóm ↔ đơn vị: phạm vi dữ liệu của group (đơn vị và các con).");

            migrationBuilder.AlterTable(
                name: "AuthorizationAuditEvents",
                oldComment: "Nhật ký thay đổi quản trị authorization (actor, hành động, entity, trace).");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "Hash SHA-256 của refresh token; không lưu plaintext.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Thời điểm revoke; null = còn hiệu lực (nếu chưa hết hạn).");

            migrationBuilder.AlterColumn<Guid>(
                name: "FamilyId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Nhóm token cùng phiên đăng nhập; revoke cả family khi phát hiện replay.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Thời điểm hết hạn; dùng cho cleanup retention.");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "PermissionDefinitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldComment: "Mã quyền ổn định dùng trong policy.");
        }
    }
}
