using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;

namespace ShuttleMate.Repositories.Context
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        #region Entity
        public virtual DbSet<Attendance> Attendances => Set<Attendance>();
        public virtual DbSet<ChatBotLog> ChatBotLogs => Set<ChatBotLog>();
        public virtual DbSet<Feedback> Feedbacks => Set<Feedback>();
        public virtual DbSet<HistoryTicket> HistoryTickets => Set<HistoryTicket>();
        public virtual DbSet<Notification> Notifications => Set<Notification>();
        public virtual DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
        public virtual DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
        public virtual DbSet<Promotion> Promotions => Set<Promotion>();
        public virtual DbSet<ResponseSupport> ResponseSupports => Set<ResponseSupport>();
        public virtual DbSet<Role> Roles => Set<Role>();
        public virtual DbSet<Route> Routes => Set<Route>();
        public virtual DbSet<RouteStop> RouteStops => Set<RouteStop>();
        public virtual DbSet<Schedule> Schedules => Set<Schedule>();
        public virtual DbSet<ScheduleOverride> ScheduleOverrides => Set<ScheduleOverride>();
        public virtual DbSet<School> Schools => Set<School>();
        public virtual DbSet<SchoolShift> SchoolShifts => Set<SchoolShift>();
        public virtual DbSet<Shuttle> Shuttles => Set<Shuttle>();
        public virtual DbSet<ShuttleLocationRecord> ShuttleLocationRecords => Set<ShuttleLocationRecord>();
        public virtual DbSet<Stop> Stops => Set<Stop>();
        public virtual DbSet<StopEstimate> StopEstimate => Set<StopEstimate>();
        public virtual DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
        public virtual DbSet<SystemLogs> SystemLogs => Set<SystemLogs>();
        public virtual DbSet<Ticket> Tickets => Set<Ticket>();
        public virtual DbSet<Transaction> Transactions => Set<Transaction>();
        public virtual DbSet<Trip> Trips => Set<Trip>();
        public virtual DbSet<User> Users => Set<User>();
        public virtual DbSet<UserRole> UserRoles => Set<UserRole>();
        public virtual DbSet<UserSchoolShift> UserSchoolShifts => Set<UserSchoolShift>();
        public virtual DbSet<Ward> Wards => Set<Ward>();
        public virtual DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
        public virtual DbSet<UserDevice> UserDevices => Set<UserDevice>();
        public virtual DbSet<ConversationSummary> ConversationSummaries => Set<ConversationSummary>();
        public virtual DbSet<HistoryTicketSchoolShift> HistoryTicketSchoolShifts => Set<HistoryTicketSchoolShift>();
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserDevice>(entity =>
            {
                entity.ToTable("UserDevice");
                entity.HasOne(u => u.User)
                    .WithMany(d => d.UserDevices)
                    .HasForeignKey(u => u.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("Attendances");
                entity.HasOne(t => t.HistoryTicket)
                    .WithMany(r => r.Attendances)
                    .HasForeignKey(t => t.HistoryTicketId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.Trip)
                    .WithMany(r => r.Attendances)
                    .HasForeignKey(t => t.TripId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.StopCheckInLocation)
                    .WithMany(r => r.AttendanceCheckInLocations)
                    .HasForeignKey(t => t.CheckInLocation)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(t => t.StopCheckOutLocation)
                    .WithMany(r => r.AttendanceCheckOutLocations)
                    .HasForeignKey(t => t.CheckOutLocation)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ChatBotLog>(entity =>
            {
                entity.ToTable("ChatBotLogs");
                entity.HasOne(t => t.User)
                    .WithMany(r => r.ChatBotLogs)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.ToTable("Feedbacks");
                entity.HasOne(t => t.User)
                    .WithMany(r => r.Feedbacks)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<HistoryTicket>(entity =>
            {
                entity.ToTable("HistoryTickets");
                entity.HasOne(t => t.Ticket)
                    .WithMany(r => r.HistoryTickets)
                    .HasForeignKey(t => t.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.User)
                    .WithMany(r => r.HistoryTickets)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ht => ht.Promotion)
                    .WithMany(p => p.HistoryTickets)
                    .HasForeignKey(ht => ht.PromotionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
            });

            modelBuilder.Entity<NotificationRecipient>(entity =>
            {
                entity.ToTable("NotificationRecipients");
                entity.HasOne(t => t.Recipient)
                    .WithMany(r => r.NotificationRecipients)
                    .HasForeignKey(t => t.RecipientId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.Notification)
                    .WithMany(r => r.NotificationRecipients)
                    .HasForeignKey(t => t.NotificationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NotificationTemplate>(entity =>
            {
                entity.ToTable("NotificationTemplates");
            });

            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("Promotions");
                entity.HasOne(t => t.Ticket)
                    .WithMany(r => r.Promotions)
                    .HasForeignKey(t => t.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<ResponseSupport>(entity =>
            {
                entity.ToTable("ResponseSupports");
                entity.HasOne(t => t.SupportRequest)
                    .WithMany(r => r.ResponseSupports)
                    .HasForeignKey(t => t.SupportRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
            });

            modelBuilder.Entity<Route>(entity =>
            {
                entity.ToTable("Routes");
                entity.HasOne(t => t.School)
                    .WithMany(r => r.Routes)
                    .HasForeignKey(t => t.SchoolId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<RouteStop>(entity =>
            {
                entity.ToTable("RouteStops");
                entity.HasKey(rs => new { rs.RouteId, rs.StopId });
                entity.HasOne(t => t.Route)
                    .WithMany(r => r.RouteStops)
                    .HasForeignKey(t => t.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.Stop)
                    .WithMany(r => r.RouteStops)
                    .HasForeignKey(t => t.StopId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.ToTable("Schedules");
                entity.HasOne(t => t.Route)
                    .WithMany(r => r.Schedules)
                    .HasForeignKey(t => t.RouteId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.SchoolShift)
                    .WithMany(r => r.Schedules)
                    .HasForeignKey(t => t.SchoolShiftId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.Shuttle)
                    .WithMany(r => r.Schedules)
                    .HasForeignKey(t => t.ShuttleId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.Driver)
                    .WithMany(r => r.Schedules)
                    .HasForeignKey(t => t.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ScheduleOverride>(entity =>
            {
                entity.ToTable("ScheduleOverrides");
                entity.HasOne(t => t.Schedule)
                    .WithMany(r => r.ScheduleOverrides)
                    .HasForeignKey(t => t.ScheduleId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(t => t.OriginalShuttle)
                    .WithMany(t => t.OriginalScheduleOverrides)
                    .HasForeignKey(t => t.OriginalShuttleId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(t => t.OverrideShuttle)
                    .WithMany(t => t.OverrideScheduleOverrides)
                    .HasForeignKey(t => t.OverrideShuttleId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(t => t.OriginalUser)
                    .WithMany(u => u.OriginalScheduleOverrides)
                    .HasForeignKey(t => t.OriginalUserId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(t => t.OverrideUser)
                    .WithMany(u => u.OverrideScheduleOverrides)
                    .HasForeignKey(t => t.OverrideUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<School>(entity =>
            {
                entity.ToTable("Schools");
            });

            modelBuilder.Entity<SchoolShift>(entity =>
            {
                entity.ToTable("SchoolShifts");
                entity.HasOne(s => s.School)
                    .WithMany(w => w.SchoolShifts)
                    .HasForeignKey(s => s.SchoolId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Shuttle>(entity =>
            {
                entity.ToTable("Shuttles");
            });

            modelBuilder.Entity<ShuttleLocationRecord>(entity =>
            {
                entity.ToTable("ShuttleLocationRecords");
                entity.HasOne(t => t.Trip)
                    .WithMany(r => r.ShuttleLocationRecords)
                    .HasForeignKey(t => t.TripId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Stop>(entity =>
            {
                entity.ToTable("Stops");

                entity.HasOne(s => s.Ward)
                    .WithMany(w => w.Stops)
                    .HasForeignKey(s => s.WardId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<StopEstimate>(entity =>
            {
                entity.ToTable("StopEstimates");
                entity.HasOne(t => t.Schedule)
                    .WithMany(r => r.StopEstimates)
                    .HasForeignKey(t => t.ScheduleId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(t => t.Stop)
                    .WithMany(r => r.StopEstimates)
                    .HasForeignKey(t => t.StopId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<SupportRequest>(entity =>
            {
                entity.ToTable("SupportRequests");
                entity.HasOne(t => t.User)
                    .WithMany(r => r.SupportRequests)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SystemLogs>(entity =>
            {
                entity.ToTable("SystemLogs");
                entity.HasOne(t => t.Actor)
                    .WithMany(r => r.SystemLogs)
                    .HasForeignKey(t => t.ActorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.ToTable("Tickets");
                entity.HasOne(t => t.Route)
                    .WithMany(r => r.Tickets)
                    .HasForeignKey(t => t.RouteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasOne(u => u.HistoryTicket)
                    .WithOne(p => p.Transaction)
                    .HasForeignKey<Transaction>(p => p.HistoryTicketId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.WithdrawalRequest)
                    .WithOne(w => w.Transaction)
                    .HasForeignKey<WithdrawalRequest>(w => w.TransactionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.ToTable("Trips");
                entity.HasMany(t => t.Feedbacks)
                    .WithOne(f => f.Trip)
                    .HasForeignKey(f => f.TripId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.Schedule)
                    .WithMany(r => r.Trips)
                    .HasForeignKey(t => t.ScheduleId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasOne(t => t.School)
                    .WithMany(s => s.Students)
                    .HasForeignKey(t => t.SchoolId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(t => t.Parent)
                    .WithMany()
                    .HasForeignKey(t => t.ParentId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });
                entity.HasOne(up => up.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(up => up.UserId);
                entity.HasOne(up => up.Role)
                    .WithMany(p => p.UserRoles)
                    .HasForeignKey(up => up.RoleId);
            });

            modelBuilder.Entity<UserSchoolShift>(entity =>
            {
                entity.ToTable("UserSchoolShifts");
                entity.HasKey(us => new { us.StudentId, us.SchoolShiftId });
                entity.HasOne(up => up.Student)
                    .WithMany(u => u.UserSchoolShifts)
                    .HasForeignKey(up => up.StudentId);
                entity.HasOne(up => up.SchoolShift)
                    .WithMany(p => p.UserSchoolShifts)
                    .HasForeignKey(up => up.SchoolShiftId);
            });

            modelBuilder.Entity<Ward>(entity =>
            {
                entity.ToTable("Wards");
            });

            modelBuilder.Entity<WithdrawalRequest>(entity =>
            {
                entity.ToTable("WithdrawalRequests");
                entity.HasOne(x => x.User)
                    .WithMany(x => x.WithdrawalRequests)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ConversationSummary>(entity =>
            {
                entity.ToTable("ConversationSummaries");
                entity.HasOne(t => t.User)
                    .WithMany(u => u.ConversationSummaries)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<HistoryTicketSchoolShift>(entity =>
            {
                entity.ToTable("HistoryTicketSchoolShifts");

                entity.HasKey(htss => new { htss.HistoryTicketId, htss.SchoolShiftId });

                entity.HasOne(htss => htss.HistoryTicket)
                    .WithMany(ht => ht.HistoryTicketSchoolShifts)
                    .HasForeignKey(htss => htss.HistoryTicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(htss => htss.SchoolShift)
                    .WithMany(ss => ss.HistoryTicketSchoolShifts)
                    .HasForeignKey(htss => htss.SchoolShiftId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
