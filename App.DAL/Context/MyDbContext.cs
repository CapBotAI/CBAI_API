using Microsoft.EntityFrameworkCore;
using App.Entities.Entities.App;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using App.Entities.Entities.Core;

namespace App.DAL.Context;

public partial class MyDbContext : IdentityDbContext<User, Role, int, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    // DbSets cho các entity mới
    public virtual DbSet<Semester> Semesters { get; set; }
    public virtual DbSet<PhaseType> PhaseTypes { get; set; }
    public virtual DbSet<Phase> Phases { get; set; }
    public virtual DbSet<TopicCategory> TopicCategories { get; set; }
    public virtual DbSet<Topic> Topics { get; set; }
    public virtual DbSet<TopicVersion> TopicVersions { get; set; }
    public virtual DbSet<LecturerSkill> LecturerSkills { get; set; }
    public virtual DbSet<Submission> Submissions { get; set; }
    public virtual DbSet<ReviewerAssignment> ReviewerAssignments { get; set; }
    public virtual DbSet<EvaluationCriteria> EvaluationCriterias { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<ReviewCriteriaScore> ReviewCriteriaScores { get; set; }
    public virtual DbSet<ReviewComment> ReviewComments { get; set; }
    public virtual DbSet<WorkflowState> WorkflowStates { get; set; }
    public virtual DbSet<WorkflowTransition> WorkflowTransitions { get; set; }
    public virtual DbSet<SubmissionWorkflowLog> SubmissionWorkflowLogs { get; set; }
    public virtual DbSet<ReviewerPerformance> ReviewerPerformances { get; set; }
    public virtual DbSet<SystemNotification> SystemNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // =============================================
        // IDENTITY ENTITIES CONFIGURATION
        // =============================================

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasMany(e => e.Claims)
                .WithOne(e => e.User)
                .HasForeignKey(uc => uc.UserId)
                .IsRequired();

            entity.HasMany(e => e.Logins)
                .WithOne(e => e.User)
                .HasForeignKey(ul => ul.UserId)
                .IsRequired();

            entity.HasMany(e => e.Tokens)
                .WithOne(e => e.User)
                .HasForeignKey(ut => ut.UserId)
                .IsRequired();

            entity.HasMany(e => e.UserRoles)
                .WithOne(e => e.User)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();
        });

        // Role Configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasMany(e => e.UserRoles)
                .WithOne(e => e.Role)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            entity.HasMany(e => e.RoleClaims)
                .WithOne(e => e.Role)
                .HasForeignKey(rc => rc.RoleId)
                .IsRequired();
        });

        // UserRole Configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .IsRequired();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .IsRequired();
        });

        // UserClaim Configuration
        modelBuilder.Entity<UserClaim>(entity =>
        {
            entity.ToTable("user_claims");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(e => e.UserId)
                .IsRequired();
        });

        // UserLogin Configuration
        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.ToTable("user_logins");
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasOne(e => e.User)
                .WithMany(u => u.Logins)
                .HasForeignKey(e => e.UserId)
                .IsRequired();
        });

        // UserToken Configuration
        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(e => e.User)
                .WithMany(u => u.Tokens)
                .HasForeignKey(e => e.UserId)
                .IsRequired();
        });

        // RoleClaim Configuration
        modelBuilder.Entity<RoleClaim>(entity =>
        {
            entity.ToTable("role_claims");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.RoleClaims)
                .HasForeignKey(e => e.RoleId)
                .IsRequired();
        });

        // =============================================
        // CONFIGURATION CHO CÁC ENTITY MỚI
        // =============================================

        // Semester Configuration
        modelBuilder.Entity<Semester>(entity =>
        {
            entity.ToTable("semesters");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // PhaseType Configuration
        modelBuilder.Entity<PhaseType>(entity =>
        {
            entity.ToTable("phase_types");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Phase Configuration
        modelBuilder.Entity<Phase>(entity =>
        {
            entity.ToTable("phases");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Semester)
                .WithMany(p => p.Phases)
                .HasForeignKey(d => d.SemesterId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.PhaseType)
                .WithMany(p => p.Phases)
                .HasForeignKey(d => d.PhaseTypeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SemesterId, e.PhaseTypeId });
            entity.HasIndex(e => e.IsActive);
        });

        // TopicCategory Configuration
        modelBuilder.Entity<TopicCategory>(entity =>
        {
            entity.ToTable("topic_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Topic Configuration
        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("topics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MaxStudents).HasDefaultValue(1);
            entity.Property(e => e.IsLegacy).HasDefaultValue(false);
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Supervisor)
                .WithMany(p => p.Topics)
                .HasForeignKey(d => d.SupervisorId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Topics)
                .HasForeignKey(d => d.CategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Semester)
                .WithMany(p => p.Topics)
                .HasForeignKey(d => d.SemesterId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SupervisorId, e.SemesterId });
            entity.HasIndex(e => new { e.SemesterId, e.IsApproved });
            entity.HasIndex(e => e.IsLegacy);
        });

        // TopicVersion Configuration
        modelBuilder.Entity<TopicVersion>(entity =>
        {
            entity.ToTable("topic_versions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.DocumentUrl).HasMaxLength(500);
            entity.Property(e => e.Status).HasDefaultValue(App.Entities.Enums.TopicStatus.Draft);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Topic)
                .WithMany(p => p.TopicVersions)
                .HasForeignKey(d => d.TopicId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.SubmittedByUser)
                .WithMany(p => p.TopicVersions)
                .HasForeignKey(d => d.SubmittedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TopicId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => new { e.TopicId, e.Status });
            entity.HasIndex(e => e.Status);
        });

        // LecturerSkill Configuration
        modelBuilder.Entity<LecturerSkill>(entity =>
        {
            entity.ToTable("lecturer_skills");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SkillTag).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ProficiencyLevel).HasDefaultValue(App.Entities.Enums.ProficiencyLevels.Intermediate);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Lecturer)
                .WithMany(p => p.LecturerSkills)
                .HasForeignKey(d => d.LecturerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.LecturerId, e.SkillTag }).IsUnique();
            entity.HasIndex(e => e.SkillTag);
        });

        // Submission Configuration
        modelBuilder.Entity<Submission>(entity =>
        {
            entity.ToTable("submissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SubmissionRound).HasDefaultValue(1);
            entity.Property(e => e.DocumentUrl).HasMaxLength(500);
            entity.Property(e => e.AiCheckStatus).HasDefaultValue(App.Entities.Enums.AiCheckStatus.Pending);
            entity.Property(e => e.AiCheckScore).HasPrecision(5, 2);
            entity.Property(e => e.Status).HasDefaultValue(App.Entities.Enums.SubmissionStatus.Pending);
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.TopicVersion)
                .WithMany(p => p.Submissions)
                .HasForeignKey(d => d.TopicVersionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Phase)
                .WithMany(p => p.Submissions)
                .HasForeignKey(d => d.PhaseId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.SubmittedByUser)
                .WithMany(p => p.Submissions)
                .HasForeignKey(d => d.SubmittedBy)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TopicVersionId, e.PhaseId, e.SubmissionRound }).IsUnique();
            entity.HasIndex(e => new { e.PhaseId, e.Status });
            entity.HasIndex(e => new { e.PhaseId, e.Status, e.SubmittedAt });
        });

        // ReviewerAssignment Configuration
        modelBuilder.Entity<ReviewerAssignment>(entity =>
        {
            entity.ToTable("reviewer_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AssignmentType).HasDefaultValue(App.Entities.Enums.AssignmentTypes.Primary);
            entity.Property(e => e.SkillMatchScore).HasPrecision(3, 2);
            entity.Property(e => e.Status).HasDefaultValue(App.Entities.Enums.AssignmentStatus.Assigned);
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Submission)
                .WithMany(p => p.ReviewerAssignments)
                .HasForeignKey(d => d.SubmissionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Reviewer)
                .WithMany(p => p.ReviewerAssignments)
                .HasForeignKey(d => d.ReviewerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.AssignedByUser)
                .WithMany()
                .HasForeignKey(d => d.AssignedBy)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SubmissionId, e.ReviewerId }).IsUnique();
            entity.HasIndex(e => new { e.ReviewerId, e.Status });
            entity.HasIndex(e => new { e.Deadline, e.Status });
        });

        // EvaluationCriteria Configuration
        modelBuilder.Entity<EvaluationCriteria>(entity =>
        {
            entity.ToTable("evaluation_criteria");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MaxScore).HasDefaultValue(10);
            entity.Property(e => e.Weight).HasPrecision(3, 2).HasDefaultValue(1.00m);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.IsActive);
        });

        // Review Configuration
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OverallScore).HasPrecision(4, 2);
            entity.Property(e => e.Recommendation).HasDefaultValue(App.Entities.Enums.ReviewRecommendations.MinorRevision);
            entity.Property(e => e.Status).HasDefaultValue(App.Entities.Enums.ReviewStatus.Draft);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Assignment)
                .WithMany(p => p.Reviews)
                .HasForeignKey(d => d.AssignmentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.AssignmentId, e.Status });
            entity.HasIndex(e => new { e.Recommendation, e.SubmittedAt });
        });

        // ReviewCriteriaScore Configuration
        modelBuilder.Entity<ReviewCriteriaScore>(entity =>
        {
            entity.ToTable("review_criteria_scores");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Score).HasPrecision(4, 2).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Review)
                .WithMany(p => p.ReviewCriteriaScores)
                .HasForeignKey(d => d.ReviewId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Criteria)
                .WithMany(p => p.ReviewCriteriaScores)
                .HasForeignKey(d => d.CriteriaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.ReviewId, e.CriteriaId }).IsUnique();
        });

        // ReviewComment Configuration
        modelBuilder.Entity<ReviewComment>(entity =>
        {
            entity.ToTable("review_comments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SectionName).HasMaxLength(100);
            entity.Property(e => e.CommentText).IsRequired();
            entity.Property(e => e.CommentType).HasDefaultValue(App.Entities.Enums.CommentTypes.Suggestion);
            entity.Property(e => e.Priority).HasDefaultValue(App.Entities.Enums.PriorityLevels.Medium);
            entity.Property(e => e.IsResolved).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Review)
                .WithMany(p => p.ReviewComments)
                .HasForeignKey(d => d.ReviewId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ReviewId, e.SectionName });
            entity.HasIndex(e => new { e.Priority, e.IsResolved });
        });

        // WorkflowState Configuration
        modelBuilder.Entity<WorkflowState>(entity =>
        {
            entity.ToTable("workflow_states");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.IsFinalState).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // WorkflowTransition Configuration
        modelBuilder.Entity<WorkflowTransition>(entity =>
        {
            entity.ToTable("workflow_transitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.FromState)
                .WithMany(p => p.FromTransitions)
                .HasForeignKey(d => d.FromStateId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ToState)
                .WithMany(p => p.ToTransitions)
                .HasForeignKey(d => d.ToStateId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.RequiredRole)
                .WithMany(p => p.WorkflowTransitions)
                .HasForeignKey(d => d.RequiredRoleId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SubmissionWorkflowLog Configuration
        modelBuilder.Entity<SubmissionWorkflowLog>(entity =>
        {
            entity.ToTable("submission_workflow_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Submission)
                .WithMany(p => p.SubmissionWorkflowLogs)
                .HasForeignKey(d => d.SubmissionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.FromState)
                .WithMany(p => p.FromStateLogs)
                .HasForeignKey(d => d.FromStateId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ToState)
                .WithMany(p => p.ToStateLogs)
                .HasForeignKey(d => d.ToStateId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ChangedByUser)
                .WithMany(p => p.SubmissionWorkflowLogs)
                .HasForeignKey(d => d.ChangedBy)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SubmissionId, e.CreatedAt });
        });

        // ReviewerPerformance Configuration
        modelBuilder.Entity<ReviewerPerformance>(entity =>
        {
            entity.ToTable("reviewer_performance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TotalAssignments).HasDefaultValue(0);
            entity.Property(e => e.CompletedAssignments).HasDefaultValue(0);
            entity.Property(e => e.AverageTimeMinutes).HasDefaultValue(0);
            entity.Property(e => e.AverageScoreGiven).HasPrecision(4, 2);
            entity.Property(e => e.OnTimeRate).HasPrecision(3, 2);
            entity.Property(e => e.QualityRating).HasPrecision(3, 2);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Reviewer)
                .WithMany(p => p.ReviewerPerformances)
                .HasForeignKey(d => d.ReviewerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Semester)
                .WithMany(p => p.ReviewerPerformances)
                .HasForeignKey(d => d.SemesterId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.ReviewerId, e.SemesterId }).IsUnique();
        });

        // SystemNotification Configuration
        modelBuilder.Entity<SystemNotification>(entity =>
        {
            entity.ToTable("system_notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Type).HasDefaultValue(App.Entities.Enums.NotificationTypes.Info);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.Property(e => e.IsRead).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User)
                .WithMany(p => p.SystemNotifications)
                .HasForeignKey(d => d.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
