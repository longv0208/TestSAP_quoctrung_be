using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FURPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "amendment_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_amendment_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "budget_expense_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    max_percentage = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_expense_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "council_remuneration_rates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    council_type = table.Column<string>(type: "text", nullable: false),
                    role_in_council = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_council_remuneration_rates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "llm_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    config_code = table.Column<string>(type: "text", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    model_name = table.Column<string>(type: "text", nullable: false),
                    system_prompt = table.Column<string>(type: "text", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_llm_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "personnel_role_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    default_coefficient = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personnel_role_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "research_tracks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_research_tracks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "research_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    max_budget_cap = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    require_ordering_unit = table.Column<bool>(type: "boolean", nullable: false),
                    require_publication = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_research_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    permissions_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rubric_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_type = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    max_total_score = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    track_id = table.Column<int>(type: "integer", nullable: true),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    applies_basic = table.Column<bool>(type: "boolean", nullable: false),
                    applies_applied = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rubric_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "semantic_search_vectors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    content_snapshot = table.Column<string>(type: "text", nullable: true),
                    content_hash = table.Column<string>(type: "text", nullable: false),
                    elasticsearch_doc_id = table.Column<string>(type: "text", nullable: true),
                    last_indexed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    index_status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_semantic_search_vectors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_financial_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_financial_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    recommended_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "budget_allocation_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    research_type_id = table.Column<int>(type: "integer", nullable: false),
                    category_code = table.Column<string>(type: "text", nullable: false),
                    category_name = table.Column<string>(type: "text", nullable: false),
                    max_percentage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_allocation_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_allocation_rules_research_types_research_type_id",
                        column: x => x.research_type_id,
                        principalTable: "research_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "disbursement_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    research_type_id = table.Column<int>(type: "integer", nullable: false),
                    round_number = table.Column<int>(type: "integer", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    condition_description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disbursement_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_disbursement_templates_research_types_research_type_id",
                        column: x => x.research_type_id,
                        principalTable: "research_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rubric_criteria",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_id = table.Column<int>(type: "integer", nullable: false),
                    criterion_name = table.Column<string>(type: "text", nullable: false),
                    max_score = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rubric_criteria", x => x.id);
                    table.ForeignKey(
                        name: "fk_rubric_criteria_rubric_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "rubric_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rubric_template_scopes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_id = table.Column<int>(type: "integer", nullable: false),
                    cycle_id = table.Column<int>(type: "integer", nullable: false),
                    track_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rubric_template_scopes", x => x.id);
                    table.ForeignKey(
                        name: "fk_rubric_template_scopes_rubric_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "rubric_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "academic_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_title = table.Column<string>(type: "text", nullable: true),
                    scientific_rank = table.Column<string>(type: "text", nullable: true),
                    degree_level = table.Column<string>(type: "text", nullable: true),
                    specialization = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "text", nullable: true),
                    hometown = table.Column<string>(type: "text", nullable: true),
                    nationality = table.Column<string>(type: "text", nullable: true),
                    gs_pgs_year = table.Column<int>(type: "integer", nullable: true),
                    gs_pgs_institution = table.Column<string>(type: "text", nullable: true),
                    isi_scopus_count = table.Column<int>(type: "integer", nullable: false),
                    intl_journal_count = table.Column<int>(type: "integer", nullable: false),
                    domestic_journal_count = table.Column<int>(type: "integer", nullable: false),
                    intl_conference_count = table.Column<int>(type: "integer", nullable: false),
                    domestic_conference_count = table.Column<int>(type: "integer", nullable: false),
                    patents_count = table.Column<int>(type: "integer", nullable: false),
                    phd_supervised_count = table.Column<int>(type: "integer", nullable: false),
                    master_supervised_count = table.Column<int>(type: "integer", nullable: false),
                    institution = table.Column<string>(type: "text", nullable: true),
                    institution_address = table.Column<string>(type: "text", nullable: true),
                    specialization_areas = table.Column<string>(type: "text", nullable: true),
                    total_invitations = table.Column<int>(type: "integer", nullable: false),
                    is_eligible_pi = table.Column<bool>(type: "boolean", nullable: false),
                    bank_account_number = table.Column<string>(type: "text", nullable: true),
                    bank_name = table.Column<string>(type: "text", nullable: true),
                    national_id = table.Column<string>(type: "text", nullable: true),
                    national_id_issued_date = table.Column<DateOnly>(type: "date", nullable: true),
                    national_id_issued_place = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_academic_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "academic_works",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    venue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    authors = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true),
                    start_year = table.Column<int>(type: "integer", nullable: true),
                    identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    volume = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    pages = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_academic_works", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "acceptance_evaluations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    council_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluator_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result = table.Column<string>(type: "text", nullable: false),
                    fail_reason = table.Column<string>(type: "text", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_valid_ballot = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_acceptance_evaluations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "amendment_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    change_description = table.Column<string>(type: "text", nullable: false),
                    change_percentage = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    justification = table.Column<string>(type: "text", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    requires_rector_approval = table.Column<bool>(type: "boolean", nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    reviewer_comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_amendment_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_amendment_requests_amendment_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "amendment_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract_disbursements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase_id = table.Column<int>(type: "integer", nullable: true),
                    round_number = table.Column<int>(type: "integer", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    planned_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    actual_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    condition_description = table.Column<string>(type: "text", nullable: false),
                    condition_met_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    condition_met_by = table.Column<Guid>(type: "uuid", nullable: true),
                    disbursed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bank_reference = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    processed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    deliverable_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_disbursements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract_phases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase_no = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_phases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract_settlements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_contracted_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_disbursed_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_returned_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    products_submitted_summary = table.Column<string>(type: "text", nullable: true),
                    accounting_cleared_at = table.Column<DateOnly>(type: "date", nullable: true),
                    assets_cleared_at = table.Column<DateOnly>(type: "date", nullable: true),
                    settlement_signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    side_a_signee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    settlement_deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_settlements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_number = table.Column<string>(type: "text", nullable: false),
                    scope_title = table.Column<string>(type: "text", nullable: true),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    original_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    max_extension_months = table.Column<int>(type: "integer", nullable: false),
                    econtract_id = table.Column<string>(type: "text", nullable: true),
                    econtract_url = table.Column<string>(type: "text", nullable: true),
                    side_a_representative = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    terminated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    terminated_reason = table.Column<string>(type: "text", nullable: true),
                    terminated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "council_decisions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    council_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_members = table.Column<int>(type: "integer", nullable: false),
                    attending_members = table.Column<int>(type: "integer", nullable: false),
                    valid_ballots = table.Column<int>(type: "integer", nullable: false),
                    invalid_ballots = table.Column<int>(type: "integer", nullable: false),
                    average_score = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    result = table.Column<string>(type: "text", nullable: false),
                    council_comments = table.Column<string>(type: "text", nullable: true),
                    recommendations = table.Column<string>(type: "text", nullable: true),
                    chair_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    secretary_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revision_request_note = table.Column<string>(type: "text", nullable: true),
                    revision_requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revision_requested_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_council_decisions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "council_member_opinions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    decision_id = table.Column<int>(type: "integer", nullable: false),
                    member_name = table.Column<string>(type: "text", nullable: false),
                    academic_comment = table.Column<string>(type: "text", nullable: true),
                    budget_comment = table.Column<string>(type: "text", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_council_member_opinions", x => x.id);
                    table.ForeignKey(
                        name: "fk_council_member_opinions_council_decisions_decision_id",
                        column: x => x.decision_id,
                        principalTable: "council_decisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "council_qa_entries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    decision_id = table.Column<int>(type: "integer", nullable: false),
                    asked_by = table.Column<string>(type: "text", nullable: true),
                    question = table.Column<string>(type: "text", nullable: false),
                    answer = table.Column<string>(type: "text", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_council_qa_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_council_qa_entries_council_decisions_decision_id",
                        column: x => x.decision_id,
                        principalTable: "council_decisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "council_meetings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    council_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    platform = table.Column<string>(type: "text", nullable: false),
                    meeting_link = table.Column<string>(type: "text", nullable: true),
                    location = table.Column<string>(type: "text", nullable: true),
                    external_meeting_id = table.Column<string>(type: "text", nullable: true),
                    calendar_event_id = table.Column<string>(type: "text", nullable: true),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    agenda = table.Column<string>(type: "text", nullable: true),
                    agenda_documents = table.Column<string>(type: "text", nullable: true),
                    actual_start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_council_meetings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "council_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    council_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_role = table.Column<string>(type: "text", nullable: false),
                    is_external = table.Column<bool>(type: "boolean", nullable: false),
                    invitation_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    invitation_token = table.Column<string>(type: "text", nullable: true),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    declined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decline_reason = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    responded_on_behalf_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_council_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meeting_attendances",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    meeting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rsvp_status = table.Column<string>(type: "text", nullable: false),
                    rsvp_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actually_attended = table.Column<bool>(type: "boolean", nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    absence_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meeting_attendances", x => x.id);
                    table.ForeignKey(
                        name: "fk_meeting_attendances_council_meetings_meeting_id",
                        column: x => x.meeting_id,
                        principalTable: "council_meetings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_meeting_attendances_council_members_member_id",
                        column: x => x.member_id,
                        principalTable: "council_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "council_project_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    council_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meeting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    slot_start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    slot_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    slot_order = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_council_project_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cycle_tracks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cycle_id = table.Column<int>(type: "integer", nullable: false),
                    track_id = table.Column<int>(type: "integer", nullable: false),
                    is_open = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cycle_tracks", x => x.id);
                    table.ForeignKey(
                        name: "fk_cycle_tracks_research_tracks_track_id",
                        column: x => x.track_id,
                        principalTable: "research_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "review_rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_track_id = table.Column<int>(type: "integer", nullable: false),
                    round_number = table.Column<int>(type: "integer", nullable: false),
                    dimension = table.Column<string>(type: "text", nullable: false),
                    round_type = table.Column<string>(type: "text", nullable: false),
                    rubric_template_id = table.Column<int>(type: "integer", nullable: true),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    prerequisite_round_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    result = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_rounds", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_rounds_cycle_tracks_cycle_track_id",
                        column: x => x.cycle_track_id,
                        principalTable: "cycle_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_rounds_review_rounds_prerequisite_round_id",
                        column: x => x.prerequisite_round_id,
                        principalTable: "review_rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_rounds_rubric_templates_rubric_template_id",
                        column: x => x.rubric_template_id,
                        principalTable: "rubric_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deadline_extensions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "text", nullable: false),
                    target_id = table.Column<string>(type: "text", nullable: false),
                    old_deadline = table.Column<DateOnly>(type: "date", nullable: false),
                    new_deadline = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deadline_extensions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    document_category = table.Column<string>(type: "text", nullable: false),
                    original_file_name = table.Column<string>(type: "text", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    storage_container = table.Column<string>(type: "text", nullable: false),
                    storage_blob_name = table.Column<string>(type: "text", nullable: false),
                    storage_url = table.Column<string>(type: "text", nullable: false),
                    is_confidential = table.Column<bool>(type: "boolean", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_email = table.Column<string>(type: "text", nullable: false),
                    email_type = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    template_name = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    provider_message_id = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "final_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_file_url = table.Column<string>(type: "text", nullable: true),
                    summary_file_url = table.Column<string>(type: "text", nullable: true),
                    language = table.Column<string>(type: "text", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    revision_requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revision_notes = table.Column<string>(type: "text", nullable: true),
                    final_submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archival_deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_final_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "llm_outputs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    output_type = table.Column<string>(type: "text", nullable: false),
                    model_used = table.Column<string>(type: "text", nullable: false),
                    prompt_version = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    tokens_input = table.Column<int>(type: "integer", nullable: true),
                    tokens_output = table.Column<int>(type: "integer", nullable: true),
                    latency_ms = table.Column<int>(type: "integer", nullable: true),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_reviewed_by_human = table.Column<bool>(type: "boolean", nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    review_notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_llm_outputs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    action_url = table.Column<string>(type: "text", nullable: true),
                    related_entity_type = table.Column<string>(type: "text", nullable: true),
                    related_entity_id = table.Column<string>(type: "text", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.CheckConstraint("CK_notifications_priority", "\"priority\" IN ('LOW', 'NORMAL', 'HIGH', 'URGENT')");
                });

            migrationBuilder.CreateTable(
                name: "organizational_units",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    unit_type = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    head_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizational_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_organizational_units_organizational_units_parent_id",
                        column: x => x.parent_id,
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    fpt_employee_id = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    fpt_sso_sub = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_external = table.Column<bool>(type: "boolean", nullable: false),
                    unit_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    password_reset_token_hash = table.Column<string>(type: "text", nullable: true),
                    password_reset_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_organizational_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_users_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "progress_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_round = table.Column<int>(type: "integer", nullable: false),
                    round_name = table.Column<string>(type: "text", nullable: true),
                    report_file_url = table.Column<string>(type: "text", nullable: true),
                    reporting_period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    reporting_period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_content = table.Column<string>(type: "text", nullable: false),
                    pending_content = table.Column<string>(type: "text", nullable: true),
                    overall_completion_pct = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    expenditure_to_date = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    next_period_plan = table.Column<string>(type: "text", nullable: true),
                    pi_recommendations = table.Column<string>(type: "text", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    scheduled_meeting_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    meeting_link = table.Column<string>(type: "text", nullable: true),
                    evaluated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    evaluated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    evaluation_result = table.Column<string>(type: "text", nullable: true),
                    evaluation_comments = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_progress_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_progress_reports_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_progress_reports_users_evaluated_by",
                        column: x => x.evaluated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_cycles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cycle_year = table.Column<int>(type: "integer", nullable: false),
                    semester_code = table.Column<string>(type: "text", nullable: true),
                    research_type_id = table.Column<int>(type: "integer", nullable: false),
                    order_collection_deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    submission_open_date = table.Column<DateOnly>(type: "date", nullable: false),
                    submission_deadline = table.Column<DateOnly>(type: "date", nullable: false),
                    review_deadline = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_research_cycles", x => x.id);
                    table.ForeignKey(
                        name: "fk_research_cycles_research_types_research_type_id",
                        column: x => x.research_type_id,
                        principalTable: "research_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_research_cycles_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "review_councils",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    council_type = table.Column<string>(type: "text", nullable: false),
                    establishment_decision_no = table.Column<string>(type: "text", nullable: true),
                    established_at = table.Column<DateOnly>(type: "date", nullable: true),
                    meeting_deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    min_members_required = table.Column<int>(type: "integer", nullable: false),
                    max_members_allowed = table.Column<int>(type: "integer", nullable: false),
                    quorum_numerator = table.Column<int>(type: "integer", nullable: false),
                    quorum_denominator = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_councils", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_councils_review_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "review_rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_councils_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_users_assigned_by",
                        column: x => x.assigned_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "progress_report_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<int>(type: "integer", nullable: false),
                    completion_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    completion_status = table.Column<string>(type: "text", nullable: false),
                    evidence_description = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_progress_report_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_progress_report_items_progress_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "progress_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_deliverables",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_phase_id = table.Column<int>(type: "integer", nullable: true),
                    category_id = table.Column<int>(type: "integer", nullable: true),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    scientific_requirements = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    trial_evidence_url = table.Column<string>(type: "text", nullable: true),
                    quality_assessment = table.Column<string>(type: "text", nullable: true),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    acceptance_status = table.Column<string>(type: "text", nullable: true),
                    sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_deliverables", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_deliverables_contract_phases_contract_phase_id",
                        column: x => x.contract_phase_id,
                        principalTable: "contract_phases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_project_deliverables_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_project_deliverables_product_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "product_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    academic_title = table.Column<string>(type: "text", nullable: true),
                    unit_name = table.Column<string>(type: "text", nullable: true),
                    work_content = table.Column<string>(type: "text", nullable: false),
                    work_months = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_pi = table.Column<bool>(type: "boolean", nullable: false),
                    is_secretary = table.Column<bool>(type: "boolean", nullable: false),
                    member_role_code = table.Column<string>(type: "text", nullable: true),
                    salary_coefficient = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_rounds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    result = table.Column<string>(type: "text", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_rounds", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_rounds_review_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "review_rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_code = table.Column<string>(type: "text", nullable: true),
                    cycle_track_id = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    pi_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hosting_unit_id = table.Column<int>(type: "integer", nullable: false),
                    research_type_id = table.Column<int>(type: "integer", nullable: false),
                    title_vi = table.Column<string>(type: "text", nullable: false),
                    title_en = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    planned_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_cycle_tracks_cycle_track_id",
                        column: x => x.cycle_track_id,
                        principalTable: "cycle_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projects_organizational_units_hosting_unit_id",
                        column: x => x.hosting_unit_id,
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projects_research_types_research_type_id",
                        column: x => x.research_type_id,
                        principalTable: "research_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projects_users_pi_user_id",
                        column: x => x.pi_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_change_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    admin_note = table.Column<string>(type: "text", nullable: true),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_change_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_change_requests_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_change_requests_users_requested_by",
                        column: x => x.requested_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_change_requests_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_review_scores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    council_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluator_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<int>(type: "integer", nullable: false),
                    general_comments = table.Column<string>(type: "text", nullable: true),
                    other_recommendations = table.Column<string>(type: "text", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_valid_ballot = table.Column<bool>(type: "boolean", nullable: false),
                    ai_feedback_suggestion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_review_scores", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_review_scores_council_members_evaluator_member_id",
                        column: x => x.evaluator_member_id,
                        principalTable: "council_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_review_scores_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_review_scores_review_councils_council_id",
                        column: x => x.council_id,
                        principalTable: "review_councils",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_review_scores_rubric_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "rubric_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_no = table.Column<int>(type: "integer", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    title_vi = table.Column<string>(type: "text", nullable: false),
                    title_en = table.Column<string>(type: "text", nullable: true),
                    duration_months = table.Column<int>(type: "integer", nullable: false),
                    planned_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    abstract_vi = table.Column<string>(type: "text", nullable: false),
                    abstract_en = table.Column<string>(type: "text", nullable: true),
                    overall_intro = table.Column<string>(type: "text", nullable: true),
                    research_objectives = table.Column<string>(type: "text", nullable: false),
                    literature_review = table.Column<string>(type: "text", nullable: true),
                    research_approach = table.Column<string>(type: "text", nullable: true),
                    methodology = table.Column<string>(type: "text", nullable: true),
                    novelty_originality = table.Column<string>(type: "text", nullable: true),
                    facilities_equipment = table.Column<string>(type: "text", nullable: true),
                    application_potential = table.Column<string>(type: "text", nullable: true),
                    transfer_potential = table.Column<string>(type: "text", nullable: true),
                    expected_output = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    funding_method = table.Column<string>(type: "text", nullable: true),
                    revision_requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revision_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposals", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposals_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposals_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cycle_id = table.Column<int>(type: "integer", nullable: false),
                    ordering_unit_id = table.Column<int>(type: "integer", nullable: false),
                    research_area = table.Column<string>(type: "text", nullable: false),
                    problem_description = table.Column<string>(type: "text", nullable: false),
                    expected_products = table.Column<string>(type: "text", nullable: true),
                    budget_cap = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    matched_project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_research_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_research_orders_organizational_units_ordering_unit_id",
                        column: x => x.ordering_unit_id,
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_research_orders_projects_matched_project_id",
                        column: x => x.matched_project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_research_orders_research_cycles_cycle_id",
                        column: x => x.cycle_id,
                        principalTable: "research_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_research_orders_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reviewer_feedbacks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    council_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    urgency_score = table.Column<int>(type: "integer", nullable: true),
                    scientific_contribution_score = table.Column<int>(type: "integer", nullable: true),
                    practical_significance_score = table.Column<int>(type: "integer", nullable: true),
                    actual_vs_expected_score = table.Column<int>(type: "integer", nullable: true),
                    other_comments = table.Column<string>(type: "text", nullable: true),
                    overall_assessment = table.Column<string>(type: "text", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviewer_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviewer_feedbacks_council_members_reviewer_member_id",
                        column: x => x.reviewer_member_id,
                        principalTable: "council_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reviewer_feedbacks_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reviewer_feedbacks_review_councils_council_id",
                        column: x => x.council_id,
                        principalTable: "review_councils",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "review_score_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    score_id = table.Column<int>(type: "integer", nullable: false),
                    criterion_id = table.Column<int>(type: "integer", nullable: false),
                    given_score = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_score_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_score_details_proposal_review_scores_score_id",
                        column: x => x.score_id,
                        principalTable: "proposal_review_scores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_score_details_rubric_criteria_criterion_id",
                        column: x => x.criterion_id,
                        principalTable: "rubric_criteria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_budget_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    source_khoan = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    source_ngoai_khoan = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    source_nsnn = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    source_other = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_budget_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_budget_items_budget_expense_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "budget_expense_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_budget_items_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_budgets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    labor_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    equipment_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    external_service_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    conference_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    office_supplies_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    incidental_ip_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_budgets", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_budgets_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_research_contents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_research_contents", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_research_contents_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_budget_labor_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_member_id = table.Column<int>(type: "integer", nullable: false),
                    total_research_hours = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    hourly_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, computedColumnSql: "\"total_research_hours\" * \"hourly_rate\"", stored: true),
                    work_days = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    coefficient = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    daily_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    proposal_budget_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_budget_labor_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_budget_labor_details_project_members_project_membe",
                        column: x => x.project_member_id,
                        principalTable: "project_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_budget_labor_details_proposal_budgets_proposal_bud",
                        column: x => x.proposal_budget_id,
                        principalTable: "proposal_budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_budget_labor_details_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_activities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    content_id = table.Column<int>(type: "integer", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_name = table.Column<string>(type: "text", nullable: false),
                    expected_result = table.Column<string>(type: "text", nullable: false),
                    start_month = table.Column<int>(type: "integer", nullable: false),
                    end_month = table.Column<int>(type: "integer", nullable: false),
                    responsible_person = table.Column<string>(type: "text", nullable: true),
                    estimated_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    activity_type = table.Column<string>(type: "text", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_activities_proposal_research_contents_content_id",
                        column: x => x.content_id,
                        principalTable: "proposal_research_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_activities_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_academic_profiles_user_id",
                table: "academic_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_academic_works_user_id_work_type",
                table: "academic_works",
                columns: new[] { "user_id", "work_type" });

            migrationBuilder.CreateIndex(
                name: "ix_acceptance_evaluations_council_id_project_id_evaluator_memb",
                table: "acceptance_evaluations",
                columns: new[] { "council_id", "project_id", "evaluator_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_acceptance_evaluations_evaluator_member_id",
                table: "acceptance_evaluations",
                column: "evaluator_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_acceptance_evaluations_project_id",
                table: "acceptance_evaluations",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_amendment_requests_category_id",
                table: "amendment_requests",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_amendment_requests_contract_id",
                table: "amendment_requests",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_amendment_requests_requested_by",
                table: "amendment_requests",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "ix_amendment_requests_reviewed_by",
                table: "amendment_requests",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_allocation_rules_research_type_id",
                table: "budget_allocation_rules",
                column: "research_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_expense_categories_code",
                table: "budget_expense_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_disbursements_condition_met_by",
                table: "contract_disbursements",
                column: "condition_met_by");

            migrationBuilder.CreateIndex(
                name: "ix_contract_disbursements_contract_id_round_number",
                table: "contract_disbursements",
                columns: new[] { "contract_id", "round_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_disbursements_deliverable_id",
                table: "contract_disbursements",
                column: "deliverable_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_disbursements_phase_id",
                table: "contract_disbursements",
                column: "phase_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_disbursements_processed_by",
                table: "contract_disbursements",
                column: "processed_by");

            migrationBuilder.CreateIndex(
                name: "ix_contract_phases_contract_id_phase_no",
                table: "contract_phases",
                columns: new[] { "contract_id", "phase_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_settlements_contract_id",
                table: "contract_settlements",
                column: "contract_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_settlements_side_a_signee_id",
                table: "contract_settlements",
                column: "side_a_signee_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_contract_number",
                table: "contracts",
                column: "contract_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_created_by",
                table: "contracts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_project_id",
                table: "contracts",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_terminated_by",
                table: "contracts",
                column: "terminated_by");

            migrationBuilder.CreateIndex(
                name: "ix_council_decisions_chair_user_id",
                table: "council_decisions",
                column: "chair_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_decisions_council_id_project_id",
                table: "council_decisions",
                columns: new[] { "council_id", "project_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_council_decisions_project_id",
                table: "council_decisions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_decisions_secretary_user_id",
                table: "council_decisions",
                column: "secretary_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_meetings_council_id",
                table: "council_meetings",
                column: "council_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_member_opinions_decision_id",
                table: "council_member_opinions",
                column: "decision_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_members_council_id",
                table: "council_members",
                column: "council_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_members_user_id",
                table: "council_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_project_assignments_council_id_project_id",
                table: "council_project_assignments",
                columns: new[] { "council_id", "project_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_council_project_assignments_project_id",
                table: "council_project_assignments",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_council_qa_entries_decision_id",
                table: "council_qa_entries",
                column: "decision_id");

            migrationBuilder.CreateIndex(
                name: "ix_cycle_tracks_cycle_id_track_id",
                table: "cycle_tracks",
                columns: new[] { "cycle_id", "track_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cycle_tracks_track_id",
                table: "cycle_tracks",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "ix_deadline_extensions_created_by",
                table: "deadline_extensions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_deadline_extensions_target_type_target_id",
                table: "deadline_extensions",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_disbursement_templates_research_type_id",
                table: "disbursement_templates",
                column: "research_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_entity",
                table: "documents",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_uploaded_by",
                table: "documents",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_recipient_user_id",
                table: "email_logs",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_final_reports_project_id",
                table: "final_reports",
                column: "project_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_llm_configs_config_code",
                table: "llm_configs",
                column: "config_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_llm_outputs_entity",
                table: "llm_outputs",
                columns: new[] { "entity_type", "entity_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_llm_outputs_reviewed_by",
                table: "llm_outputs",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_meeting_attendances_meeting_id_member_id",
                table: "meeting_attendances",
                columns: new[] { "meeting_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meeting_attendances_member_id",
                table: "meeting_attendances",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_unread",
                table: "notifications",
                columns: new[] { "user_id", "is_read", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_code",
                table: "organizational_units",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_head_user_id",
                table: "organizational_units",
                column: "head_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_parent_id",
                table: "organizational_units",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_personnel_role_types_code",
                table: "personnel_role_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_progress_report_items_activity_id",
                table: "progress_report_items",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "ix_progress_report_items_report_id",
                table: "progress_report_items",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_progress_reports_contract_id_report_round",
                table: "progress_reports",
                columns: new[] { "contract_id", "report_round" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_progress_reports_evaluated_by",
                table: "progress_reports",
                column: "evaluated_by");

            migrationBuilder.CreateIndex(
                name: "ix_project_deliverables_category_id",
                table: "project_deliverables",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_deliverables_contract_id",
                table: "project_deliverables",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_deliverables_contract_phase_id",
                table: "project_deliverables",
                column: "contract_phase_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_deliverables_project_id",
                table: "project_deliverables",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_members_project_id",
                table: "project_members",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_members_user_id",
                table: "project_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_rounds_project_id_round_id",
                table: "project_rounds",
                columns: new[] { "project_id", "round_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_rounds_round_id",
                table: "project_rounds",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_cycle_track_id",
                table: "projects",
                column: "cycle_track_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_hosting_unit_id",
                table: "projects",
                column: "hosting_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_order_id",
                table: "projects",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_pi_user_id",
                table: "projects",
                column: "pi_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_project_code",
                table: "projects",
                column: "project_code",
                unique: true,
                filter: "\"project_code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_projects_research_type_id",
                table: "projects",
                column: "research_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_activities_content_id",
                table: "proposal_activities",
                column: "content_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_activities_proposal_id",
                table: "proposal_activities",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_budget_items_category_id",
                table: "proposal_budget_items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_budget_items_proposal_id",
                table: "proposal_budget_items",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_budget_labor_details_project_member_id",
                table: "proposal_budget_labor_details",
                column: "project_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_budget_labor_details_proposal_budget_id",
                table: "proposal_budget_labor_details",
                column: "proposal_budget_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_budget_labor_details_proposal_id",
                table: "proposal_budget_labor_details",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_budgets_proposal_id",
                table: "proposal_budgets",
                column: "proposal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proposal_change_requests_project_id",
                table: "proposal_change_requests",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_change_requests_requested_by",
                table: "proposal_change_requests",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_change_requests_reviewed_by",
                table: "proposal_change_requests",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_research_contents_proposal_id",
                table: "proposal_research_contents",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_review_scores_council_id_project_id_evaluator_memb",
                table: "proposal_review_scores",
                columns: new[] { "council_id", "project_id", "evaluator_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proposal_review_scores_evaluator_member_id",
                table: "proposal_review_scores",
                column: "evaluator_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_review_scores_project_id",
                table: "proposal_review_scores",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_review_scores_template_id",
                table: "proposal_review_scores",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_approved_by",
                table: "proposals",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_project_id_version_no",
                table: "proposals",
                columns: new[] { "project_id", "version_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_research_cycles_created_by",
                table: "research_cycles",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_research_cycles_research_type_id",
                table: "research_cycles",
                column: "research_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_orders_created_by",
                table: "research_orders",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_research_orders_cycle_id",
                table: "research_orders",
                column: "cycle_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_orders_matched_project_id",
                table: "research_orders",
                column: "matched_project_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_orders_ordering_unit_id",
                table: "research_orders",
                column: "ordering_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_tracks_code",
                table: "research_tracks",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_research_types_code",
                table: "research_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_councils_created_by",
                table: "review_councils",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_review_councils_round_id",
                table: "review_councils",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_rounds_cycle_track_id_round_number_dimension",
                table: "review_rounds",
                columns: new[] { "cycle_track_id", "round_number", "dimension" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_rounds_prerequisite_round_id",
                table: "review_rounds",
                column: "prerequisite_round_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_rounds_rubric_template_id",
                table: "review_rounds",
                column: "rubric_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_score_details_criterion_id",
                table: "review_score_details",
                column: "criterion_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_score_details_score_id",
                table: "review_score_details",
                column: "score_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_feedbacks_council_id_project_id_reviewer_member_id",
                table: "reviewer_feedbacks",
                columns: new[] { "council_id", "project_id", "reviewer_member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_feedbacks_project_id",
                table: "reviewer_feedbacks",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviewer_feedbacks_reviewer_member_id",
                table: "reviewer_feedbacks",
                column: "reviewer_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_rubric_criteria_template_id",
                table: "rubric_criteria",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_rubric_template_scopes_template_id_cycle_id_track_id",
                table: "rubric_template_scopes",
                columns: new[] { "template_id", "cycle_id", "track_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_semantic_search_vectors_entity_type_entity_id",
                table: "semantic_search_vectors",
                columns: new[] { "entity_type", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_financial_configs_code",
                table: "system_financial_configs",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_key",
                table: "system_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_assigned_by",
                table: "user_roles",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_deleted_by",
                table: "users",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_unit_id",
                table: "users",
                column: "unit_id");

            migrationBuilder.AddForeignKey(
                name: "fk_academic_profiles_users_user_id",
                table: "academic_profiles",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_academic_works_users_user_id",
                table: "academic_works",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_acceptance_evaluations_council_members_evaluator_member_id",
                table: "acceptance_evaluations",
                column: "evaluator_member_id",
                principalTable: "council_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_acceptance_evaluations_projects_project_id",
                table: "acceptance_evaluations",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_acceptance_evaluations_review_councils_council_id",
                table: "acceptance_evaluations",
                column: "council_id",
                principalTable: "review_councils",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_amendment_requests_contracts_contract_id",
                table: "amendment_requests",
                column: "contract_id",
                principalTable: "contracts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_amendment_requests_users_requested_by",
                table: "amendment_requests",
                column: "requested_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_amendment_requests_users_reviewed_by",
                table: "amendment_requests",
                column: "reviewed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_users_user_id",
                table: "audit_logs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contract_disbursements_contract_phases_phase_id",
                table: "contract_disbursements",
                column: "phase_id",
                principalTable: "contract_phases",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contract_disbursements_contracts_contract_id",
                table: "contract_disbursements",
                column: "contract_id",
                principalTable: "contracts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contract_disbursements_project_deliverables_deliverable_id",
                table: "contract_disbursements",
                column: "deliverable_id",
                principalTable: "project_deliverables",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contract_disbursements_users_condition_met_by",
                table: "contract_disbursements",
                column: "condition_met_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contract_disbursements_users_processed_by",
                table: "contract_disbursements",
                column: "processed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contract_phases_contracts_contract_id",
                table: "contract_phases",
                column: "contract_id",
                principalTable: "contracts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contract_settlements_contracts_contract_id",
                table: "contract_settlements",
                column: "contract_id",
                principalTable: "contracts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contract_settlements_users_side_a_signee_id",
                table: "contract_settlements",
                column: "side_a_signee_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contracts_projects_project_id",
                table: "contracts",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contracts_users_created_by",
                table: "contracts",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contracts_users_terminated_by",
                table: "contracts",
                column: "terminated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_decisions_projects_project_id",
                table: "council_decisions",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_decisions_review_councils_council_id",
                table: "council_decisions",
                column: "council_id",
                principalTable: "review_councils",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_decisions_users_chair_user_id",
                table: "council_decisions",
                column: "chair_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_decisions_users_secretary_user_id",
                table: "council_decisions",
                column: "secretary_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_meetings_review_councils_council_id",
                table: "council_meetings",
                column: "council_id",
                principalTable: "review_councils",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_members_review_councils_council_id",
                table: "council_members",
                column: "council_id",
                principalTable: "review_councils",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_members_users_user_id",
                table: "council_members",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_project_assignments_projects_project_id",
                table: "council_project_assignments",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_council_project_assignments_review_councils_council_id",
                table: "council_project_assignments",
                column: "council_id",
                principalTable: "review_councils",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cycle_tracks_research_cycles_cycle_id",
                table: "cycle_tracks",
                column: "cycle_id",
                principalTable: "research_cycles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_deadline_extensions_users_created_by",
                table: "deadline_extensions",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_documents_users_uploaded_by",
                table: "documents",
                column: "uploaded_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_email_logs_users_recipient_user_id",
                table: "email_logs",
                column: "recipient_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_final_reports_projects_project_id",
                table: "final_reports",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_llm_outputs_users_reviewed_by",
                table: "llm_outputs",
                column: "reviewed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_users_user_id",
                table: "notifications",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_organizational_units_users_head_user_id",
                table: "organizational_units",
                column: "head_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_progress_report_items_proposal_activities_activity_id",
                table: "progress_report_items",
                column: "activity_id",
                principalTable: "proposal_activities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_project_deliverables_projects_project_id",
                table: "project_deliverables",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_project_members_projects_project_id",
                table: "project_members",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_project_rounds_projects_project_id",
                table: "project_rounds",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_projects_research_orders_order_id",
                table: "projects",
                column: "order_id",
                principalTable: "research_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organizational_units_users_head_user_id",
                table: "organizational_units");

            migrationBuilder.DropForeignKey(
                name: "fk_projects_users_pi_user_id",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "fk_research_cycles_users_created_by",
                table: "research_cycles");

            migrationBuilder.DropForeignKey(
                name: "fk_research_orders_users_created_by",
                table: "research_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_research_orders_projects_matched_project_id",
                table: "research_orders");

            migrationBuilder.DropTable(
                name: "academic_profiles");

            migrationBuilder.DropTable(
                name: "academic_works");

            migrationBuilder.DropTable(
                name: "acceptance_evaluations");

            migrationBuilder.DropTable(
                name: "amendment_requests");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "budget_allocation_rules");

            migrationBuilder.DropTable(
                name: "contract_disbursements");

            migrationBuilder.DropTable(
                name: "contract_settlements");

            migrationBuilder.DropTable(
                name: "council_member_opinions");

            migrationBuilder.DropTable(
                name: "council_project_assignments");

            migrationBuilder.DropTable(
                name: "council_qa_entries");

            migrationBuilder.DropTable(
                name: "council_remuneration_rates");

            migrationBuilder.DropTable(
                name: "deadline_extensions");

            migrationBuilder.DropTable(
                name: "disbursement_templates");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "email_logs");

            migrationBuilder.DropTable(
                name: "final_reports");

            migrationBuilder.DropTable(
                name: "llm_configs");

            migrationBuilder.DropTable(
                name: "llm_outputs");

            migrationBuilder.DropTable(
                name: "meeting_attendances");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "personnel_role_types");

            migrationBuilder.DropTable(
                name: "progress_report_items");

            migrationBuilder.DropTable(
                name: "project_rounds");

            migrationBuilder.DropTable(
                name: "proposal_budget_items");

            migrationBuilder.DropTable(
                name: "proposal_budget_labor_details");

            migrationBuilder.DropTable(
                name: "proposal_change_requests");

            migrationBuilder.DropTable(
                name: "review_score_details");

            migrationBuilder.DropTable(
                name: "reviewer_feedbacks");

            migrationBuilder.DropTable(
                name: "rubric_template_scopes");

            migrationBuilder.DropTable(
                name: "semantic_search_vectors");

            migrationBuilder.DropTable(
                name: "system_financial_configs");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "amendment_categories");

            migrationBuilder.DropTable(
                name: "project_deliverables");

            migrationBuilder.DropTable(
                name: "council_decisions");

            migrationBuilder.DropTable(
                name: "council_meetings");

            migrationBuilder.DropTable(
                name: "progress_reports");

            migrationBuilder.DropTable(
                name: "proposal_activities");

            migrationBuilder.DropTable(
                name: "budget_expense_categories");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "proposal_budgets");

            migrationBuilder.DropTable(
                name: "proposal_review_scores");

            migrationBuilder.DropTable(
                name: "rubric_criteria");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "contract_phases");

            migrationBuilder.DropTable(
                name: "product_categories");

            migrationBuilder.DropTable(
                name: "proposal_research_contents");

            migrationBuilder.DropTable(
                name: "council_members");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "proposals");

            migrationBuilder.DropTable(
                name: "review_councils");

            migrationBuilder.DropTable(
                name: "review_rounds");

            migrationBuilder.DropTable(
                name: "rubric_templates");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "cycle_tracks");

            migrationBuilder.DropTable(
                name: "research_orders");

            migrationBuilder.DropTable(
                name: "research_tracks");

            migrationBuilder.DropTable(
                name: "organizational_units");

            migrationBuilder.DropTable(
                name: "research_cycles");

            migrationBuilder.DropTable(
                name: "research_types");
        }
    }
}
