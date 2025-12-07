using System;
using Listed.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listed.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "listed");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:listed.event_status", "cancelled,draft,finished,published")
                .Annotation("Npgsql:Enum:listed.organisation_role", "admin,owner")
                .Annotation("Npgsql:Enum:listed.participation_status", "accepted,invited,rejected,requested");

            migrationBuilder.CreateTable(
                name: "organisations",
                schema: "listed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    corporate_identity_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organisations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "listed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_algorithm = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    password_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_soft_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                schema: "listed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    lower_age_limit = table.Column<int>(type: "integer", nullable: false),
                    upper_age_limit = table.Column<int>(type: "integer", nullable: true),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<EventStatus>(type: "listed.event_status", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_events__organisations_organisation_id",
                        column: x => x.organisation_id,
                        principalSchema: "listed",
                        principalTable: "organisations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organisation_photos",
                schema: "listed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organisation_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_organisation_photos_organisations_organisation_id",
                        column: x => x.organisation_id,
                        principalSchema: "listed",
                        principalTable: "organisations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organisation_members",
                schema: "listed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<OrganisationRole>(type: "listed.organisation_role", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organisation_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_organisation_members__users_user_id",
                        column: x => x.user_id,
                        principalSchema: "listed",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organisation_members_organisations_organisation_id",
                        column: x => x.organisation_id,
                        principalSchema: "listed",
                        principalTable: "organisations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_infos",
                schema: "listed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nationality = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    national_identification_number = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    first_name = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    has_phone_prefix = table.Column<bool>(type: "boolean", nullable: false),
                    biography = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_infos", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_infos_users_id",
                        column: x => x.id,
                        principalSchema: "listed",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_photos",
                schema: "listed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_photos_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "listed",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_participants",
                schema: "listed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<ParticipationStatus>(type: "listed.participation_status", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_participants__users_user_id",
                        column: x => x.user_id,
                        principalSchema: "listed",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_participants_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "listed",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_photos",
                schema: "listed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_photos_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "listed",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "index_event_participants_event_id",
                schema: "listed",
                table: "event_participants",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_user_id",
                schema: "listed",
                table: "event_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "unique_index_event_participants_event_id_user_id",
                schema: "listed",
                table: "event_participants",
                columns: new[] { "event_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "index_event_photos_event_id",
                schema: "listed",
                table: "event_photos",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "index_events_organisation_id",
                schema: "listed",
                table: "events",
                column: "organisation_id");

            migrationBuilder.CreateIndex(
                name: "ix_organisation_members_user_id",
                schema: "listed",
                table: "organisation_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "unique_index_organisation_members_organisation_id_user_id",
                schema: "listed",
                table: "organisation_members",
                columns: new[] { "organisation_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "index_organisation_photos_organisation_id",
                schema: "listed",
                table: "organisation_photos",
                column: "organisation_id");

            migrationBuilder.CreateIndex(
                name: "unique_index_organisations_country_cin",
                schema: "listed",
                table: "organisations",
                columns: new[] { "country", "corporate_identity_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "unique_index_users_nin",
                schema: "listed",
                table: "user_infos",
                column: "national_identification_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "unique_index_users_phone_number",
                schema: "listed",
                table: "user_infos",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "index_user_photos_user_id",
                schema: "listed",
                table: "user_photos",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "unique_index_users_email",
                schema: "listed",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_participants",
                schema: "listed");

            migrationBuilder.DropTable(
                name: "event_photos",
                schema: "listed");

            migrationBuilder.DropTable(
                name: "organisation_members",
                schema: "listed");

            migrationBuilder.DropTable(
                name: "organisation_photos",
                schema: "listed");

            migrationBuilder.DropTable(
                name: "user_infos",
                schema: "listed");

            migrationBuilder.DropTable(
                name: "user_photos",
                schema: "listed");

            migrationBuilder.DropTable(
                name: "events",
                schema: "listed");

            migrationBuilder.DropTable(
                name: "users",
                schema: "listed");

            migrationBuilder.DropTable(
                name: "organisations",
                schema: "listed");
        }
    }
}
