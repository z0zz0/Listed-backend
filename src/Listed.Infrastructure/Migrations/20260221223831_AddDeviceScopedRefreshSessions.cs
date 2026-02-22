using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceScopedRefreshSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "device_id",
                schema: "listed",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "unique_index_refresh_tokens_user_id_device_id_active",
                schema: "listed",
                table: "refresh_tokens",
                columns: new[] { "user_id", "device_id" },
                unique: true,
                filter: "\"revoked_at\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "unique_index_refresh_tokens_user_id_device_id_active",
                schema: "listed",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "device_id",
                schema: "listed",
                table: "refresh_tokens");
        }
    }
}
