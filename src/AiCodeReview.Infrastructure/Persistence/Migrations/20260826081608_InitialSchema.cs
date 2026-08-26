using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCodeReview.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "background_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    available_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    locked_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_background_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    github_user_id = table.Column<long>(type: "bigint", nullable: false),
                    login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    last_signed_in_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "code_repositories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    github_repository_id = table.Column<long>(type: "bigint", nullable: false),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    default_branch = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    primary_language = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_code_repositories", x => x.id);
                    table.ForeignKey(
                        name: "fk_code_repositories_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "github_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_token_protected = table.Column<byte[]>(type: "bytea", nullable: false),
                    refresh_token_protected = table.Column<byte[]>(type: "bytea", nullable: true),
                    token_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    connected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_github_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_github_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pull_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    github_pull_request_id = table.Column<long>(type: "bigint", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    author_login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_draft = table.Column<bool>(type: "boolean", nullable: false),
                    head_sha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    base_sha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    head_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    base_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    additions = table.Column<int>(type: "integer", nullable: false),
                    deletions = table.Column<int>(type: "integer", nullable: false),
                    changed_files = table.Column<int>(type: "integer", nullable: false),
                    github_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_synced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pull_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_pull_requests_code_repositories_code_repository_id",
                        column: x => x.code_repository_id,
                        principalTable: "code_repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pull_request_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pull_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    head_sha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    previous_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    change_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    additions = table.Column<int>(type: "integer", nullable: false),
                    deletions = table.Column<int>(type: "integer", nullable: false),
                    blob_sha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    is_binary = table.Column<bool>(type: "boolean", nullable: false),
                    is_truncated = table.Column<bool>(type: "boolean", nullable: false),
                    patch = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pull_request_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_pull_request_files_pull_requests_pull_request_id",
                        column: x => x.pull_request_id,
                        principalTable: "pull_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pull_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    head_sha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    trigger = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    risk_score = table.Column<int>(type: "integer", nullable: true),
                    risk_band = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    score_breakdown_json = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    queued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_runs_code_repositories_code_repository_id",
                        column: x => x.code_repository_id,
                        principalTable: "code_repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_review_runs_pull_requests_pull_request_id",
                        column: x => x.pull_request_id,
                        principalTable: "pull_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_review_runs_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_usages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false),
                    cached_input_tokens = table.Column<int>(type: "integer", nullable: false),
                    estimated_cost_usd = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_usages_review_runs_review_run_id",
                        column: x => x.review_run_id,
                        principalTable: "review_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_findings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    confidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    rule_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    start_line = table.Column<int>(type: "integer", nullable: false),
                    end_line = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    reasoning = table.Column<string>(type: "text", nullable: true),
                    recommendation = table.Column<string>(type: "text", nullable: true),
                    suggested_fix = table.Column<string>(type: "text", nullable: true),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status_changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status_changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_suppressed = table.Column<bool>(type: "boolean", nullable: false),
                    suppression_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_findings", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_findings_code_repositories_code_repository_id",
                        column: x => x.code_repository_id,
                        principalTable: "code_repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_review_findings_review_runs_review_run_id",
                        column: x => x.review_run_id,
                        principalTable: "review_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_review_findings_users_status_changed_by_user_id",
                        column: x => x.status_changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_usages_created_at_utc",
                table: "ai_usages",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_ai_usages_review_run_id",
                table: "ai_usages",
                column: "review_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_background_jobs_claimable",
                table: "background_jobs",
                column: "available_at_utc",
                filter: "state = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_background_jobs_expiring_leases",
                table: "background_jobs",
                column: "locked_until_utc",
                filter: "state = 'Running'");

            migrationBuilder.CreateIndex(
                name: "ix_code_repositories_owner_user_id",
                table: "code_repositories",
                column: "owner_user_id",
                filter: "deleted_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_code_repositories_owner_user_id_github_repository_id",
                table: "code_repositories",
                columns: new[] { "owner_user_id", "github_repository_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_github_accounts_user_id",
                table: "github_accounts",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pull_request_files_pull_request_id_head_sha_path",
                table: "pull_request_files",
                columns: new[] { "pull_request_id", "head_sha", "path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pull_requests_code_repository_id_github_updated_at_utc",
                table: "pull_requests",
                columns: new[] { "code_repository_id", "github_updated_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_pull_requests_code_repository_id_number",
                table: "pull_requests",
                columns: new[] { "code_repository_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_findings_code_repository_id_fingerprint",
                table: "review_findings",
                columns: new[] { "code_repository_id", "fingerprint" });

            migrationBuilder.CreateIndex(
                name: "ix_review_findings_code_repository_id_severity_created_at_utc",
                table: "review_findings",
                columns: new[] { "code_repository_id", "severity", "created_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_review_findings_review_run_id_fingerprint",
                table: "review_findings",
                columns: new[] { "review_run_id", "fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_findings_status_changed_by_user_id",
                table: "review_findings",
                column: "status_changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_runs_active_per_commit",
                table: "review_runs",
                columns: new[] { "pull_request_id", "head_sha" },
                unique: true,
                filter: "status IN ('Queued', 'Running')");

            migrationBuilder.CreateIndex(
                name: "ix_review_runs_code_repository_id",
                table: "review_runs",
                column: "code_repository_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_runs_pull_request_id_queued_at_utc",
                table: "review_runs",
                columns: new[] { "pull_request_id", "queued_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_review_runs_requested_by_user_id",
                table: "review_runs",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_github_user_id",
                table: "users",
                column: "github_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_usages");

            migrationBuilder.DropTable(
                name: "background_jobs");

            migrationBuilder.DropTable(
                name: "github_accounts");

            migrationBuilder.DropTable(
                name: "pull_request_files");

            migrationBuilder.DropTable(
                name: "review_findings");

            migrationBuilder.DropTable(
                name: "review_runs");

            migrationBuilder.DropTable(
                name: "pull_requests");

            migrationBuilder.DropTable(
                name: "code_repositories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
