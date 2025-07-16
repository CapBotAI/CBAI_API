using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evaluation_criteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxScore = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    Weight = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 1.00m),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_criteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "phase_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phase_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "semesters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_semesters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "topic_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topic_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_states",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFinalState = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_states", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_claims_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    PhaseTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmissionDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_phases_phase_types_PhaseTypeId",
                        column: x => x.PhaseTypeId,
                        principalTable: "phase_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_phases_semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lecturer_skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LecturerId = table.Column<int>(type: "int", nullable: false),
                    SkillTag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProficiencyLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lecturer_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lecturer_skills_users_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviewer_performance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewerId = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    TotalAssignments = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CompletedAssignments = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AverageTimeMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AverageScoreGiven = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    OnTimeRate = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    QualityRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviewer_performance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reviewer_performance_semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reviewer_performance_users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "system_notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_system_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Objectives = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupervisorId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    MaxStudents = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsLegacy = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_topics_semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_topics_topic_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "topic_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_topics_users_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_claims_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_user_logins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_user_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_transitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromStateId = table.Column<int>(type: "int", nullable: true),
                    ToStateId = table.Column<int>(type: "int", nullable: false),
                    RequiredRoleId = table.Column<int>(type: "int", nullable: true),
                    ConditionRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_transitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_transitions_roles_RequiredRoleId",
                        column: x => x.RequiredRoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_transitions_workflow_states_FromStateId",
                        column: x => x.FromStateId,
                        principalTable: "workflow_states",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_transitions_workflow_states_ToStateId",
                        column: x => x.ToStateId,
                        principalTable: "workflow_states",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "topic_versions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Objectives = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Methodology = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedOutcomes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topic_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_topic_versions_topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_topic_versions_users_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicVersionId = table.Column<int>(type: "int", nullable: false),
                    PhaseId = table.Column<int>(type: "int", nullable: false),
                    SubmittedBy = table.Column<int>(type: "int", nullable: false),
                    SubmissionRound = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiCheckStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AiCheckScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    AiCheckDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submissions_phases_PhaseId",
                        column: x => x.PhaseId,
                        principalTable: "phases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_submissions_topic_versions_TopicVersionId",
                        column: x => x.TopicVersionId,
                        principalTable: "topic_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_submissions_users_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reviewer_assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    ReviewerId = table.Column<int>(type: "int", nullable: false),
                    AssignedBy = table.Column<int>(type: "int", nullable: false),
                    AssignmentType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SkillMatchScore = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviewer_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reviewer_assignments_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reviewer_assignments_users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reviewer_assignments_users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "submission_workflow_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    FromStateId = table.Column<int>(type: "int", nullable: true),
                    ToStateId = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_workflow_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submission_workflow_logs_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_submission_workflow_logs_users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_submission_workflow_logs_workflow_states_FromStateId",
                        column: x => x.FromStateId,
                        principalTable: "workflow_states",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_submission_workflow_logs_workflow_states_ToStateId",
                        column: x => x.ToStateId,
                        principalTable: "workflow_states",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    OverallComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recommendation = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    TimeSpentMinutes = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reviews_reviewer_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "reviewer_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewId = table.Column<int>(type: "int", nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: true),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommentType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_review_comments_reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_criteria_scores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewId = table.Column<int>(type: "int", nullable: false),
                    CriteriaId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_criteria_scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_review_criteria_scores_evaluation_criteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "evaluation_criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_review_criteria_scores_reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_criteria_IsActive",
                table: "evaluation_criteria",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_lecturer_skills_LecturerId_SkillTag",
                table: "lecturer_skills",
                columns: new[] { "LecturerId", "SkillTag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lecturer_skills_SkillTag",
                table: "lecturer_skills",
                column: "SkillTag");

            migrationBuilder.CreateIndex(
                name: "IX_phases_IsActive",
                table: "phases",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_phases_PhaseTypeId",
                table: "phases",
                column: "PhaseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_phases_SemesterId_PhaseTypeId",
                table: "phases",
                columns: new[] { "SemesterId", "PhaseTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_review_comments_Priority_IsResolved",
                table: "review_comments",
                columns: new[] { "Priority", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_review_comments_ReviewId_SectionName",
                table: "review_comments",
                columns: new[] { "ReviewId", "SectionName" });

            migrationBuilder.CreateIndex(
                name: "IX_review_criteria_scores_CriteriaId",
                table: "review_criteria_scores",
                column: "CriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_review_criteria_scores_ReviewId_CriteriaId",
                table: "review_criteria_scores",
                columns: new[] { "ReviewId", "CriteriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviewer_assignments_AssignedBy",
                table: "reviewer_assignments",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_reviewer_assignments_Deadline_Status",
                table: "reviewer_assignments",
                columns: new[] { "Deadline", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_reviewer_assignments_ReviewerId_Status",
                table: "reviewer_assignments",
                columns: new[] { "ReviewerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_reviewer_assignments_SubmissionId_ReviewerId",
                table: "reviewer_assignments",
                columns: new[] { "SubmissionId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviewer_performance_ReviewerId_SemesterId",
                table: "reviewer_performance",
                columns: new[] { "ReviewerId", "SemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviewer_performance_SemesterId",
                table: "reviewer_performance",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_AssignmentId_Status",
                table: "reviews",
                columns: new[] { "AssignmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_reviews_Recommendation_SubmittedAt",
                table: "reviews",
                columns: new[] { "Recommendation", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_role_claims_RoleId",
                table: "role_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_semesters_IsActive",
                table: "semesters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_semesters_Name",
                table: "semesters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submission_workflow_logs_ChangedBy",
                table: "submission_workflow_logs",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_submission_workflow_logs_FromStateId",
                table: "submission_workflow_logs",
                column: "FromStateId");

            migrationBuilder.CreateIndex(
                name: "IX_submission_workflow_logs_SubmissionId_CreatedAt",
                table: "submission_workflow_logs",
                columns: new[] { "SubmissionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_submission_workflow_logs_ToStateId",
                table: "submission_workflow_logs",
                column: "ToStateId");

            migrationBuilder.CreateIndex(
                name: "IX_submissions_PhaseId_Status",
                table: "submissions",
                columns: new[] { "PhaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_submissions_PhaseId_Status_SubmittedAt",
                table: "submissions",
                columns: new[] { "PhaseId", "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_submissions_SubmittedBy",
                table: "submissions",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_submissions_TopicVersionId_PhaseId_SubmissionRound",
                table: "submissions",
                columns: new[] { "TopicVersionId", "PhaseId", "SubmissionRound" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_notifications_UserId_IsRead_CreatedAt",
                table: "system_notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_topic_versions_Status",
                table: "topic_versions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_topic_versions_SubmittedBy",
                table: "topic_versions",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_topic_versions_TopicId_Status",
                table: "topic_versions",
                columns: new[] { "TopicId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_topic_versions_TopicId_VersionNumber",
                table: "topic_versions",
                columns: new[] { "TopicId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_topics_CategoryId",
                table: "topics",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_topics_IsLegacy",
                table: "topics",
                column: "IsLegacy");

            migrationBuilder.CreateIndex(
                name: "IX_topics_SemesterId_IsApproved",
                table: "topics",
                columns: new[] { "SemesterId", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_topics_SupervisorId_SemesterId",
                table: "topics",
                columns: new[] { "SupervisorId", "SemesterId" });

            migrationBuilder.CreateIndex(
                name: "IX_user_claims_UserId",
                table: "user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_logins_UserId",
                table: "user_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_transitions_FromStateId",
                table: "workflow_transitions",
                column: "FromStateId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_transitions_RequiredRoleId",
                table: "workflow_transitions",
                column: "RequiredRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_transitions_ToStateId",
                table: "workflow_transitions",
                column: "ToStateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lecturer_skills");

            migrationBuilder.DropTable(
                name: "review_comments");

            migrationBuilder.DropTable(
                name: "review_criteria_scores");

            migrationBuilder.DropTable(
                name: "reviewer_performance");

            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "submission_workflow_logs");

            migrationBuilder.DropTable(
                name: "system_notifications");

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_logins");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropTable(
                name: "workflow_transitions");

            migrationBuilder.DropTable(
                name: "evaluation_criteria");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "workflow_states");

            migrationBuilder.DropTable(
                name: "reviewer_assignments");

            migrationBuilder.DropTable(
                name: "submissions");

            migrationBuilder.DropTable(
                name: "phases");

            migrationBuilder.DropTable(
                name: "topic_versions");

            migrationBuilder.DropTable(
                name: "phase_types");

            migrationBuilder.DropTable(
                name: "topics");

            migrationBuilder.DropTable(
                name: "semesters");

            migrationBuilder.DropTable(
                name: "topic_categories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
