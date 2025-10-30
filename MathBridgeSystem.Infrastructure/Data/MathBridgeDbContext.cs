using System;
using System.Collections.Generic;
using MathBridgeSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MathBridgeSystem.Infrastructure.Data;

public partial class MathBridgeDbContext : DbContext
{
    public MathBridgeDbContext()
    {
    }

    public MathBridgeDbContext(DbContextOptions<MathBridgeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Center> Centers { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<Child> Children { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Curriculum> Curricula { get; set; }

    public virtual DbSet<FinalFeedback> FinalFeedbacks { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PaymentGatewayConfig> PaymentGatewayConfigs { get; set; }

    public virtual DbSet<PaymentPackage> PaymentPackages { get; set; }

    public virtual DbSet<PayosTransaction> PayosTransactions { get; set; }

    public virtual DbSet<RescheduleRequest> RescheduleRequests { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<SepayTransaction> SepayTransactions { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<SupportRequest> SupportRequests { get; set; }

    public virtual DbSet<TestResult> TestResults { get; set; }

    public virtual DbSet<TutorCenter> TutorCenters { get; set; }

    public virtual DbSet<TutorSchedule> TutorSchedules { get; set; }

    public virtual DbSet<TutorVerification> TutorVerifications { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VideoConferenceSession> VideoConferenceSessions { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=sql.vibe88.tech;Database=mathbridge;User Id=sa;Password=Phineas160404;MultipleActiveResultSets=true;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Center>(entity =>
        {
            entity.HasKey(e => e.CenterId).HasName("PK__centers__290A2887526C5A12");

            entity.ToTable("centers");

            entity.HasIndex(e => e.City, "IX_Centers_City");

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_Centers_Location");

            entity.HasIndex(e => e.Name, "ix_centers_name");

            entity.Property(e => e.CenterId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("center_id");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(2)
                .HasDefaultValue("VN")
                .HasColumnName("country_code");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.FormattedAddress)
                .HasMaxLength(500)
                .HasColumnName("formatted_address");
            entity.Property(e => e.GooglePlaceId)
                .HasMaxLength(255)
                .HasColumnName("google_place_id");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.LocationUpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("location_updated_date");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PlaceName)
                .HasMaxLength(255)
                .HasColumnName("place_name");
            entity.Property(e => e.TutorCount).HasColumnName("tutor_count");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__chat_mes__0BBF6EE69D14EC58");

            entity.ToTable("chat_messages");

            entity.HasIndex(e => e.RecipientUserId, "IX_chat_messages_recipient_user_id");

            entity.HasIndex(e => e.SentDate, "IX_chat_messages_sent_date");

            entity.HasIndex(e => e.UserId, "IX_chat_messages_user_id");

            entity.Property(e => e.MessageId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("message_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.MessageText).HasColumnName("message_text");
            entity.Property(e => e.MessageType)
                .HasMaxLength(20)
                .HasDefaultValue("text")
                .HasColumnName("message_type");
            entity.Property(e => e.RecipientUserId).HasColumnName("recipient_user_id");
            entity.Property(e => e.SentDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("sent_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.RecipientUser).WithMany(p => p.ChatMessageRecipientUsers)
                .HasForeignKey(d => d.RecipientUserId)
                .HasConstraintName("FK_chat_messages_recipient_user_id");

            entity.HasOne(d => d.User).WithMany(p => p.ChatMessageUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_chat_messages_user_id");
        });

        modelBuilder.Entity<Child>(entity =>
        {
            entity.HasKey(e => e.ChildId).HasName("PK__children__015ADC05C21463A1");

            entity.ToTable("children");

            entity.HasIndex(e => e.SchoolId, "IX_children_school");

            entity.HasIndex(e => e.CenterId, "ix_children_center_id");

            entity.HasIndex(e => e.ParentId, "ix_children_parent_id");

            entity.HasIndex(e => e.Status, "ix_children_status");

            entity.Property(e => e.ChildId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("child_id");
            entity.Property(e => e.CenterId).HasColumnName("center_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.CurrentTopic)
                .HasMaxLength(200)
                .HasColumnName("current_topic");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Grade)
                .HasMaxLength(50)
                .HasColumnName("grade");
            entity.Property(e => e.LastTopicUpdate)
                .HasColumnType("datetime")
                .HasColumnName("last_topic_update");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");

            entity.HasOne(d => d.Center).WithMany(p => p.Children)
                .HasForeignKey(d => d.CenterId)
                .HasConstraintName("fk_children_center");

            entity.HasOne(d => d.Parent).WithMany(p => p.Children)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_children_users");

            entity.HasOne(d => d.School).WithMany(p => p.Children)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_children_schools");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__contract__F8D66423932B1126");

            entity.ToTable("contracts");

            entity.HasIndex(e => e.VideoCallPlatform, "IX_contract_video_platform");

            entity.HasIndex(e => e.CenterId, "ix_contracts_center_id");

            entity.HasIndex(e => e.ChildId, "ix_contracts_child_id");

            entity.HasIndex(e => e.MainTutorId, "ix_contracts_main_tutor_id");

            entity.HasIndex(e => e.PackageId, "ix_contracts_package_id");

            entity.HasIndex(e => e.ParentId, "ix_contracts_parent_id");

            entity.Property(e => e.ContractId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("contract_id");
            entity.Property(e => e.CenterId).HasColumnName("center_id");
            entity.Property(e => e.ChildId).HasColumnName("child_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.DaysOfWeeks).HasColumnName("days_of_weeks");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.IsOnline).HasColumnName("is_online");
            entity.Property(e => e.MainTutorId).HasColumnName("main_tutor_id");
            entity.Property(e => e.MaxDistanceKm)
                .HasDefaultValue(7.00m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("max_distance_km");
            entity.Property(e => e.OfflineAddress)
                .HasMaxLength(500)
                .HasColumnName("offline_address");
            entity.Property(e => e.OfflineLatitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("offline_latitude");
            entity.Property(e => e.OfflineLongitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("offline_longitude");
            entity.Property(e => e.PackageId).HasColumnName("package_id");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.RescheduleCount).HasColumnName("reschedule_count");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.SubstituteTutor1Id).HasColumnName("substitute_tutor1_id");
            entity.Property(e => e.SubstituteTutor2Id).HasColumnName("substitute_tutor2_id");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
            entity.Property(e => e.VideoCallPlatform)
                .HasMaxLength(50)
                .HasColumnName("video_call_platform");

            entity.HasOne(d => d.Center).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.CenterId)
                .HasConstraintName("fk_contracts_center");

            entity.HasOne(d => d.Child).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.ChildId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contracts_child");

            entity.HasOne(d => d.MainTutor).WithMany(p => p.ContractMainTutors)
                .HasForeignKey(d => d.MainTutorId)
                .HasConstraintName("fk_contracts_main_tutor");

            entity.HasOne(d => d.Package).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contracts_package");

            entity.HasOne(d => d.Parent).WithMany(p => p.ContractParents)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contracts_parent");

            entity.HasOne(d => d.SubstituteTutor1).WithMany(p => p.ContractSubstituteTutor1s)
                .HasForeignKey(d => d.SubstituteTutor1Id)
                .HasConstraintName("fk_contracts_substitute_tutor1");

            entity.HasOne(d => d.SubstituteTutor2).WithMany(p => p.ContractSubstituteTutor2s)
                .HasForeignKey(d => d.SubstituteTutor2Id)
                .HasConstraintName("fk_contracts_substitute_tutor2");
        });

        modelBuilder.Entity<Curriculum>(entity =>
        {
            entity.HasKey(e => e.CurriculumId).HasName("PK__curricul__17583C76352BF625");

            entity.ToTable("curriculum");

            entity.HasIndex(e => e.CurriculumCode, "IX_curriculum_code");

            entity.HasIndex(e => e.CurriculumCode, "UQ__curricul__575F76872A623D21").IsUnique();

            entity.Property(e => e.CurriculumId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("curriculum_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.CurriculumCode)
                .HasMaxLength(20)
                .HasColumnName("curriculum_code");
            entity.Property(e => e.CurriculumName)
                .HasMaxLength(100)
                .HasColumnName("curriculum_name");
            entity.Property(e => e.Grades)
                .HasMaxLength(20)
                .HasColumnName("grades");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SyllabusUrl)
                .HasMaxLength(500)
                .HasColumnName("syllabus_url");
            entity.Property(e => e.TotalCredits).HasColumnName("total_credits");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<FinalFeedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__final_fe__7A6B2B8C88297D2D");

            entity.ToTable("final_feedback");

            entity.HasIndex(e => e.ContractId, "IX_final_feedback_contract_id");

            entity.HasIndex(e => e.OverallSatisfactionRating, "IX_final_feedback_overall_rating");

            entity.HasIndex(e => e.FeedbackProviderType, "IX_final_feedback_provider_type");

            entity.HasIndex(e => e.UserId, "IX_final_feedback_user_id");

            entity.Property(e => e.FeedbackId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("feedback_id");
            entity.Property(e => e.AdditionalComments)
                .HasMaxLength(1000)
                .HasColumnName("additional_comments");
            entity.Property(e => e.CommunicationRating).HasColumnName("communication_rating");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.ContractObjectivesMet).HasColumnName("contract_objectives_met");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.FeedbackProviderType)
                .HasMaxLength(20)
                .HasColumnName("feedback_provider_type");
            entity.Property(e => e.FeedbackStatus)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("feedback_status");
            entity.Property(e => e.FeedbackText).HasColumnName("feedback_text");
            entity.Property(e => e.ImprovementSuggestions)
                .HasMaxLength(1000)
                .HasColumnName("improvement_suggestions");
            entity.Property(e => e.LearningProgressRating).HasColumnName("learning_progress_rating");
            entity.Property(e => e.OverallSatisfactionRating).HasColumnName("overall_satisfaction_rating");
            entity.Property(e => e.ProfessionalismRating).HasColumnName("professionalism_rating");
            entity.Property(e => e.SessionQualityRating).HasColumnName("session_quality_rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WouldRecommend).HasColumnName("would_recommend");
            entity.Property(e => e.WouldWorkTogetherAgain).HasColumnName("would_work_together_again");

            entity.HasOne(d => d.Contract).WithMany(p => p.FinalFeedbacks)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_final_feedback_contract_id");

            entity.HasOne(d => d.User).WithMany(p => p.FinalFeedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_final_feedback_user_id");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__notifica__E059842F41695738");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.BookingId, "ix_notifications_booking_id");

            entity.HasIndex(e => e.ContractId, "ix_notifications_contract_id");

            entity.HasIndex(e => e.UserId, "ix_notifications_user_id");

            entity.Property(e => e.NotificationId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("notification_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .HasColumnName("notification_type");
            entity.Property(e => e.SentDate)
                .HasColumnType("datetime")
                .HasColumnName("sent_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("fk_notifications_booking");

            entity.HasOne(d => d.Contract).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("fk_notifications_contract");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_notifications_user");
        });

        modelBuilder.Entity<PaymentGatewayConfig>(entity =>
        {
            entity.HasKey(e => e.GatewayId).HasName("PK__payment___0AF5B00B1B274D31");

            entity.ToTable("payment_gateway_config");

            entity.HasIndex(e => e.GatewayName, "UQ_PaymentGatewayConfig_GatewayName").IsUnique();

            entity.Property(e => e.GatewayId).HasColumnName("gateway_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasColumnName("display_name");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.GatewayName)
                .HasMaxLength(50)
                .HasColumnName("gateway_name");
            entity.Property(e => e.IconUrl).HasColumnName("icon_url");
            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true)
                .HasColumnName("is_enabled");
            entity.Property(e => e.MaxAmount)
                .HasDefaultValue(50000000m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("max_amount");
            entity.Property(e => e.MinAmount)
                .HasDefaultValue(10000m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("min_amount");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<PaymentPackage>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__payment___63846AE8C179A8BE");

            entity.ToTable("payment_packages");

            entity.HasIndex(e => e.CurriculumId, "IX_payment_packages_curriculum");

            entity.Property(e => e.PackageId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("package_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.CurriculumId).HasColumnName("curriculum_id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.DurationDays).HasColumnName("duration_days");
            entity.Property(e => e.Grade)
                .HasMaxLength(50)
                .HasColumnName("grade");
            entity.Property(e => e.MaxReschedule).HasColumnName("max_reschedule");
            entity.Property(e => e.PackageName)
                .HasMaxLength(100)
                .HasColumnName("package_name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.SessionCount).HasColumnName("session_count");
            entity.Property(e => e.SessionsPerWeek).HasColumnName("sessions_per_week");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");

            entity.HasOne(d => d.Curriculum).WithMany(p => p.PaymentPackages)
                .HasForeignKey(d => d.CurriculumId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_payment_packages_curriculum");
        });

        modelBuilder.Entity<PayosTransaction>(entity =>
        {
            entity.HasKey(e => e.PayosTransactionId).HasName("PK__payos_tr__11A5DD8D5AA3D100");

            entity.ToTable("payos_transactions");

            entity.HasIndex(e => e.CreatedDate, "IX_PayOSTransactions_CreatedDate").IsDescending();

            entity.HasIndex(e => e.OrderCode, "IX_PayOSTransactions_OrderCode").IsUnique();

            entity.HasIndex(e => e.PaymentStatus, "IX_PayOSTransactions_PaymentStatus");

            entity.HasIndex(e => e.WalletTransactionId, "IX_PayOSTransactions_WalletTransactionId");

            entity.Property(e => e.PayosTransactionId)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("payos_transaction_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CancelUrl).HasColumnName("cancel_url");
            entity.Property(e => e.CheckoutUrl).HasColumnName("checkout_url");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.OrderCode).HasColumnName("order_code");
            entity.Property(e => e.PaidAt)
                .HasColumnType("datetime")
                .HasColumnName("paid_at");
            entity.Property(e => e.PaymentLinkId)
                .HasMaxLength(255)
                .HasColumnName("payment_link_id");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasColumnName("payment_status");
            entity.Property(e => e.ReturnUrl).HasColumnName("return_url");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
            entity.Property(e => e.WalletTransactionId).HasColumnName("wallet_transaction_id");

            entity.HasOne(d => d.Contract).WithMany(p => p.PayosTransactions)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("contract_link");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.PayosTransactions)
                .HasForeignKey(d => d.WalletTransactionId)
                .HasConstraintName("FK_PayOSTransactions_WalletTransactions");
        });

        modelBuilder.Entity<RescheduleRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__reschedu__18D3B90F1BFC4B87");

            entity.ToTable("reschedule_requests");

            entity.HasIndex(e => e.BookingId, "ix_reschedule_requests_booking_id");

            entity.HasIndex(e => e.ParentId, "ix_reschedule_requests_parent_id");

            entity.Property(e => e.RequestId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("request_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.ProcessedDate)
                .HasColumnType("datetime")
                .HasColumnName("processed_date");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.RequestedDate).HasColumnName("requested_date");
            entity.Property(e => e.RequestedTimeSlot)
                .HasMaxLength(50)
                .HasColumnName("requested_time_slot");
            entity.Property(e => e.RequestedTutorId).HasColumnName("requested_tutor_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.RescheduleRequests)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_reschedule_requests_booking");

            entity.HasOne(d => d.Parent).WithMany(p => p.RescheduleRequestParents)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_reschedule_requests_parent");

            entity.HasOne(d => d.RequestedTutor).WithMany(p => p.RescheduleRequestRequestedTutors)
                .HasForeignKey(d => d.RequestedTutorId)
                .HasConstraintName("fk_reschedule_requests_tutor");

            entity.HasOne(d => d.Staff).WithMany(p => p.RescheduleRequestStaffs)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("fk_reschedule_requests_staff");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__reviews__60883D90D1F42160");

            entity.ToTable("reviews");

            entity.HasIndex(e => e.Rating, "IX_reviews_rating");

            entity.HasIndex(e => e.UserId, "IX_reviews_user_id");

            entity.Property(e => e.ReviewId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("review_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ReviewStatus)
                .HasMaxLength(20)
                .HasDefaultValue("approved")
                .HasColumnName("review_status");
            entity.Property(e => e.ReviewText)
                .HasMaxLength(2000)
                .HasColumnName("review_text");
            entity.Property(e => e.ReviewTitle)
                .HasMaxLength(200)
                .HasColumnName("review_title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("reviews_users_user_id_fk");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__roles__760965CC23BBB9E9");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "UQ__roles__783254B1E80F97AE").IsUnique();

            entity.HasIndex(e => e.RoleName, "ix_roles_name");

            entity.Property(e => e.RoleId)
                .ValueGeneratedNever()
                .HasColumnName("role_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.SchoolId).HasName("PK__schools__27CA6CF4957EF47B");

            entity.ToTable("schools");

            entity.HasIndex(e => e.CurriculumId, "IX_schools_curriculum");

            entity.HasIndex(e => e.SchoolName, "IX_schools_name");

            entity.Property(e => e.SchoolId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("school_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.CurriculumId).HasColumnName("curriculum_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SchoolName)
                .HasMaxLength(255)
                .HasColumnName("school_name");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");

            entity.HasOne(d => d.Curriculum).WithMany(p => p.Schools)
                .HasForeignKey(d => d.CurriculumId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_schools_curriculum");
        });

        modelBuilder.Entity<SepayTransaction>(entity =>
        {
            entity.HasKey(e => e.SepayTransactionId).HasName("PK_SePayTransactions");

            entity.ToTable("sepay_transactions");

            entity.HasIndex(e => e.ContractId, "idx_sepay_transactions_contract_id").HasFilter("([contract_id] IS NOT NULL)");

            entity.HasIndex(e => e.WalletTransactionId, "idx_sepay_transactions_wallet_transaction_id").HasFilter("([wallet_transaction_id] IS NOT NULL)");

            entity.HasIndex(e => e.Code, "ix_sepay_transactions_code").IsUnique();

            entity.HasIndex(e => e.TransactionDate, "ix_sepay_transactions_date");

            entity.HasIndex(e => e.OrderReference, "ix_sepay_transactions_order_reference");

            entity.HasIndex(e => e.WalletTransactionId, "ix_sepay_transactions_wallet_transaction_id");

            entity.Property(e => e.SepayTransactionId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("sepay_transaction_id");
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(50)
                .HasColumnName("account_number");
            entity.Property(e => e.Accumulated)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("accumulated");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.Content)
                .HasMaxLength(500)
                .HasColumnName("content");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");
            entity.Property(e => e.Gateway)
                .HasMaxLength(50)
                .HasColumnName("gateway");
            entity.Property(e => e.OrderReference)
                .HasMaxLength(50)
                .HasColumnName("order_reference");
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(100)
                .HasColumnName("reference_number");
            entity.Property(e => e.SubAccount)
                .HasMaxLength(50)
                .HasColumnName("sub_account");
            entity.Property(e => e.TransactionDate).HasColumnName("transaction_date");
            entity.Property(e => e.TransferAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("transfer_amount");
            entity.Property(e => e.TransferType)
                .HasMaxLength(10)
                .HasColumnName("transfer_type");
            entity.Property(e => e.WalletTransactionId).HasColumnName("wallet_transaction_id");

            entity.HasOne(d => d.Contract).WithMany(p => p.SepayTransactions)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("fk_sepay_transactions_contract");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.SepayTransactions)
                .HasForeignKey(d => d.WalletTransactionId)
                .HasConstraintName("fk_sepay_transactions_wallet_transaction");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__schedule__5DE3A5B16338CF16");

            entity.ToTable("sessions");

            entity.HasIndex(e => e.VideoCallPlatform, "IX_session_video_platform");

            entity.HasIndex(e => e.ContractId, "ix_booking_sessions_contract_id");

            entity.HasIndex(e => e.Status, "ix_booking_sessions_status");

            entity.HasIndex(e => e.TutorId, "ix_booking_sessions_tutor_id");

            entity.Property(e => e.BookingId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("booking_id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.IsOnline).HasColumnName("is_online");
            entity.Property(e => e.OfflineAddress)
                .HasMaxLength(500)
                .HasColumnName("offline_address");
            entity.Property(e => e.OfflineLatitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("offline_latitude");
            entity.Property(e => e.OfflineLongitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("offline_longitude");
            entity.Property(e => e.SessionDate).HasColumnName("session_date");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.VideoCallLink)
                .HasMaxLength(255)
                .HasColumnName("video_call_link");
            entity.Property(e => e.VideoCallPlatform)
                .HasMaxLength(50)
                .HasColumnName("video_call_platform");

            entity.HasOne(d => d.Contract).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_booking_sessions_contract");

            entity.HasOne(d => d.Tutor).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_booking_sessions_tutors");
        });

        modelBuilder.Entity<SupportRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__support___18D3B90FDFBEA4AD");

            entity.ToTable("support_requests");

            entity.HasIndex(e => e.AssignedToUserId, "IX_support_requests_assigned_to_user_id");

            entity.HasIndex(e => e.Category, "IX_support_requests_category");

            entity.HasIndex(e => e.CreatedDate, "IX_support_requests_created_date");

            entity.HasIndex(e => e.Status, "IX_support_requests_status");

            entity.HasIndex(e => e.UserId, "IX_support_requests_user_id");

            entity.Property(e => e.RequestId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("request_id");
            entity.Property(e => e.AdminNotes)
                .HasMaxLength(1000)
                .HasColumnName("admin_notes");
            entity.Property(e => e.AssignedToUserId).HasColumnName("assigned_to_user_id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Resolution).HasColumnName("resolution");
            entity.Property(e => e.ResolvedDate)
                .HasColumnType("datetime")
                .HasColumnName("resolved_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("open")
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(200)
                .HasColumnName("subject");
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.SupportRequestAssignedToUsers)
                .HasForeignKey(d => d.AssignedToUserId)
                .HasConstraintName("FK_support_requests_assigned_to_user_id");

            entity.HasOne(d => d.User).WithMany(p => p.SupportRequestUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_support_requests_user_id");
        });

        modelBuilder.Entity<TestResult>(entity =>
        {
            entity.HasKey(e => e.ResultId).HasName("PK__test_res__AFB3C316774AFA46");

            entity.ToTable("test_results");

            entity.HasIndex(e => e.ChildId, "IX_test_results_child_id");

            entity.HasIndex(e => e.CurriculumId, "IX_test_results_curriculum");

            entity.HasIndex(e => e.Percentage, "IX_test_results_percentage");

            entity.HasIndex(e => e.TestDate, "IX_test_results_test_date");

            entity.HasIndex(e => e.TutorId, "IX_test_results_tutor_id");

            entity.Property(e => e.ResultId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("result_id");
            entity.Property(e => e.AreasForImprovement)
                .HasMaxLength(500)
                .HasColumnName("areas_for_improvement");
            entity.Property(e => e.ChildId).HasColumnName("child_id");
            entity.Property(e => e.CorrectAnswers).HasColumnName("correct_answers");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.CurriculumId).HasColumnName("curriculum_id");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.MaxScore)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("max_score");
            entity.Property(e => e.Notes)
                .HasMaxLength(1000)
                .HasColumnName("notes");
            entity.Property(e => e.NumberOfQuestions).HasColumnName("number_of_questions");
            entity.Property(e => e.Percentage)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("percentage");
            entity.Property(e => e.Score)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("score");
            entity.Property(e => e.TestDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("test_date");
            entity.Property(e => e.TestName)
                .HasMaxLength(200)
                .HasColumnName("test_name");
            entity.Property(e => e.TestType)
                .HasMaxLength(50)
                .HasColumnName("test_type");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_date");

            entity.HasOne(d => d.Child).WithMany(p => p.TestResults)
                .HasForeignKey(d => d.ChildId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_test_results_child_id");

            entity.HasOne(d => d.Curriculum).WithMany(p => p.TestResults)
                .HasForeignKey(d => d.CurriculumId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_test_results_curriculum");

            entity.HasOne(d => d.Tutor).WithMany(p => p.TestResults)
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_test_results_tutor_id");
        });

        modelBuilder.Entity<TutorCenter>(entity =>
        {
            entity.HasKey(e => e.TutorCenterId).HasName("PK__tutor_ce__E4F33A88509B8636");

            entity.ToTable("tutor_centers");

            entity.HasIndex(e => e.CenterId, "ix_tutor_centers_center_id");

            entity.HasIndex(e => e.TutorId, "ix_tutor_centers_tutor_id");

            entity.HasIndex(e => new { e.TutorId, e.CenterId }, "uq_tutor_center").IsUnique();

            entity.Property(e => e.TutorCenterId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("tutor_center_id");
            entity.Property(e => e.CenterId).HasColumnName("center_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");

            entity.HasOne(d => d.Center).WithMany(p => p.TutorCenters)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tutor_centers_center");

            entity.HasOne(d => d.Tutor).WithMany(p => p.TutorCenters)
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tutor_centers_tutor");
        });

        modelBuilder.Entity<TutorSchedule>(entity =>
        {
            entity.HasKey(e => e.AvailabilityId).HasName("PK__tutor_av__86E3A80163D5EEF8");

            entity.ToTable("tutor_schedule");

            entity.Property(e => e.AvailabilityId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("availability_id");
            entity.Property(e => e.AvailableFrom).HasColumnName("available_from");
            entity.Property(e => e.AvailableUntil).HasColumnName("available_until");
            entity.Property(e => e.CanTeachOffline)
                .HasDefaultValue(true)
                .HasColumnName("can_teach_offline");
            entity.Property(e => e.CanTeachOnline)
                .HasDefaultValue(true)
                .HasColumnName("can_teach_online");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.DaysOfWeek).HasColumnName("days_of_week");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveUntil).HasColumnName("effective_until");
            entity.Property(e => e.IsBooked).HasColumnName("is_booked");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");

            entity.HasOne(d => d.Tutor).WithMany(p => p.TutorSchedules)
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tutor_ava__tutor__0618D7E0");
        });

        modelBuilder.Entity<TutorVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("PK__tutor_ve__24F1796982ADF4CD");

            entity.ToTable("tutor_verifications");

            entity.HasIndex(e => e.UserId, "idx_tutor_verifications_user_id").IsUnique();

            entity.HasIndex(e => e.VerificationStatus, "ix_tutor_verifications_verification_status");

            entity.Property(e => e.VerificationId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("verification_id");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.HourlyRate)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("hourly_rate");
            entity.Property(e => e.Major)
                .HasMaxLength(255)
                .HasColumnName("major");
            entity.Property(e => e.University)
                .HasMaxLength(255)
                .HasColumnName("university");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerificationDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("verification_date");
            entity.Property(e => e.VerificationStatus)
                .HasMaxLength(20)
                .HasColumnName("verification_status");

            entity.HasOne(d => d.User).WithOne(p => p.TutorVerification)
                .HasForeignKey<TutorVerification>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tutor_verifications_users");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("PK__units__D3AF5BD7ADCFE7F5");

            entity.ToTable("units");

            entity.HasIndex(e => new { e.CurriculumId, e.UnitName }, "UQ_units_curriculum_name").IsUnique();

            entity.Property(e => e.UnitId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("unit_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.Credit)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("credit");
            entity.Property(e => e.CurriculumId).HasColumnName("curriculum_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LearningObjectives).HasColumnName("learning_objectives");
            entity.Property(e => e.UnitDescription)
                .HasMaxLength(500)
                .HasColumnName("unit_description");
            entity.Property(e => e.UnitName)
                .HasMaxLength(100)
                .HasColumnName("unit_name");
            entity.Property(e => e.UnitOrder).HasColumnName("unit_order");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.UnitCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_units_created_by");

            entity.HasOne(d => d.Curriculum).WithMany(p => p.Units)
                .HasForeignKey(d => d.CurriculumId)
                .HasConstraintName("FK_units_curriculum");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.UnitUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_units_updated_by");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370F4678BC13");

            entity.ToTable("users");

            entity.HasIndex(e => new { e.City, e.District }, "IX_users_city_district").HasFilter("([city] IS NOT NULL)");

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_users_location_coordinates");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E616496FFE2D6").IsUnique();

            entity.HasIndex(e => e.Email, "ix_users_email");

            entity.HasIndex(e => e.RoleId, "ix_users_role_id");

            entity.HasIndex(e => e.Status, "ix_users_status");

            entity.Property(e => e.UserId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("user_id");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(2)
                .HasDefaultValue("VN")
                .HasColumnName("country_code");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FormattedAddress)
                .HasMaxLength(500)
                .HasComment("Complete formatted address from Google Places API")
                .HasColumnName("formatted_address");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.GooglePlaceId)
                .HasMaxLength(255)
                .HasComment("Google Places API place identifier for location accuracy")
                .HasColumnName("google_place_id");
            entity.Property(e => e.LastActive)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("last_active");
            entity.Property(e => e.Latitude)
                .HasComment("GPS latitude coordinate for distance calculations")
                .HasColumnName("latitude");
            entity.Property(e => e.LocationUpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("location_updated_date");
            entity.Property(e => e.Longitude)
                .HasComment("GPS longitude coordinate for distance calculations")
                .HasColumnName("longitude");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.PlaceName)
                .HasMaxLength(200)
                .HasColumnName("place_name");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.WalletBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("wallet_balance");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_users_roles");
        });

        modelBuilder.Entity<VideoConferenceSession>(entity =>
        {
            entity.HasKey(e => e.ConferenceId).HasName("PK__video_co__DC92030833EE47F6");

            entity.ToTable("video_conference_sessions");

            entity.HasIndex(e => e.BookingId, "IX_video_conference_booking");

            entity.HasIndex(e => e.ContractId, "IX_video_conference_contract");

            entity.HasIndex(e => e.Platform, "IX_video_conference_platform");

            entity.HasIndex(e => e.Status, "IX_video_conference_status");

            entity.Property(e => e.ConferenceId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("conference_id");
            entity.Property(e => e.ActualEndTime)
                .HasColumnType("datetime")
                .HasColumnName("actual_end_time");
            entity.Property(e => e.ActualStartTime)
                .HasColumnType("datetime")
                .HasColumnName("actual_start_time");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(255)
                .HasColumnName("display_name");
            entity.Property(e => e.MeetingCode)
                .HasMaxLength(50)
                .HasColumnName("meeting_code");
            entity.Property(e => e.MeetingUri)
                .HasMaxLength(500)
                .HasColumnName("meeting_uri");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .HasColumnName("platform");
            entity.Property(e => e.ScheduledEndTime)
                .HasColumnType("datetime")
                .HasColumnName("scheduled_end_time");
            entity.Property(e => e.ScheduledStartTime)
                .HasColumnType("datetime")
                .HasColumnName("scheduled_start_time");
            entity.Property(e => e.SpaceId)
                .HasMaxLength(255)
                .HasColumnName("space_id");
            entity.Property(e => e.SpaceName)
                .HasMaxLength(500)
                .HasColumnName("space_name");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("scheduled")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");

            entity.HasOne(d => d.Booking).WithMany(p => p.VideoConferenceSessions)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_video_conference_booking");

            entity.HasOne(d => d.Contract).WithMany(p => p.VideoConferenceSessions)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_video_conference_contract");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.VideoConferenceSessions)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_video_conference_creator");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__wallet_t__85C600AFCEAB3D23");

            entity.ToTable("wallet_transactions");

            entity.HasIndex(e => e.PaymentGateway, "IX_WalletTransactions_PaymentGateway");

            entity.HasIndex(e => e.ContractId, "ix_wallet_transactions_contract_id");

            entity.HasIndex(e => e.ParentId, "ix_wallet_transactions_parent_id");

            entity.HasIndex(e => e.Status, "ix_wallet_transactions_status");

            entity.Property(e => e.TransactionId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("transaction_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.PaymentGateway)
                .HasMaxLength(50)
                .HasColumnName("payment_gateway");
            entity.Property(e => e.PaymentGatewayReference)
                .HasMaxLength(100)
                .HasColumnName("payment_gateway_reference");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("transaction_date");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .HasColumnName("transaction_type");

            entity.HasOne(d => d.Contract).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("fk_wallet_transactions_contract");

            entity.HasOne(d => d.Parent).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wallet_transactions_parent");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
