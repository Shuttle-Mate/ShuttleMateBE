using AutoMapper;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Abstractions;
using ShuttleMate.Contract.Repositories.Entities;

namespace ShuttleMate.Repositories.Context
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        #region Entity
        public virtual DbSet<User> Users => Set<User>();
        public virtual DbSet<Role> Roles => Set<Role>();
        public virtual DbSet<UserRole> UserRoles => Set<UserRole>();
        public virtual DbSet<Shuttle> Shuttles => Set<Shuttle>();
        public virtual DbSet<Route> Routes => Set<Route>();
        public virtual DbSet<Stop> Stops => Set<Stop>();
        public virtual DbSet<Ward> Wards => Set<Ward>();
        public virtual DbSet<DepartureTime> DepartureTimes => Set<DepartureTime>();
        public virtual DbSet<StopEstimate> StopEstimate => Set<StopEstimate>();
        public virtual DbSet<Attendance> Attendances => Set<Attendance>();
        public virtual DbSet<TicketType> TicketTypes => Set<TicketType>();
        public virtual DbSet<Trip> Trips => Set<Trip>();
        public virtual DbSet<Promotion> Promotions => Set<Promotion>();
        public virtual DbSet<TicketPromotion> TicketPromotions => Set<TicketPromotion>();
        public virtual DbSet<UserPromotion> UserPromotions => Set<UserPromotion>();
        public virtual DbSet<Feedback> Feedbacks => Set<Feedback>();
        public virtual DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
        public virtual DbSet<ShuttleLocationRecord> ShuttleLocationRecords => Set<ShuttleLocationRecord>();
        public virtual DbSet<ChatBotLog> ChatBotLogs => Set<ChatBotLog>();
        public virtual DbSet<HistoryTicket> HistoryTickets => Set<HistoryTicket>();
        public virtual DbSet<Notification> Notifications => Set<Notification>();
        public virtual DbSet<Transaction> Transactions => Set<Transaction>();
        public virtual DbSet<SystemLogs> SystemLogs => Set<SystemLogs>();
        public virtual DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
        public virtual DbSet<ResponseSupport> ResponseSupports => Set<ResponseSupport>();
        public virtual DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
        public virtual DbSet<RouteStop> RouteStops => Set<RouteStop>();
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                // Khóa ngoại School
                entity.HasOne(t => t.School)
                    .WithMany(s=>s.Students) 
                    .HasForeignKey(t => t.SchoolId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Khóa ngoại Parent
                entity.HasOne(t => t.Parent)
                    .WithMany()
                    .HasForeignKey(t => t.ParentId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
            });

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });


            modelBuilder.Entity<Shuttle>(entity =>
            {
                entity.ToTable("Shuttles");
                // Khóa ngoại Operator/Driver
                entity.HasOne(t => t.User)
                    .WithMany(r => r.Shuttles)
                    .HasForeignKey(t => t.OperatorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Route>(entity =>
            {
                entity.ToTable("Routes");
                // Khóa ngoại School
                entity.HasOne(t => t.User)
                    .WithMany(r => r.Routes)
                    .HasForeignKey(t => t.SchoolId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<RouteStop>()
                    .HasKey(rs => new { rs.RouteId, rs.StopId });

            modelBuilder.Entity<RouteStop>(entity =>
            {
                entity.ToTable("RouteStops");
                // Khóa ngoại Route
                entity.HasOne(t => t.Route)
                    .WithMany(r => r.RouteStops)
                    .HasForeignKey(t => t.RouteId)
                    .OnDelete(DeleteBehavior.Cascade);
                // Khóa ngoại Stop
                entity.HasOne(t => t.Stop)
                    .WithMany(r => r.RouteStops)
                    .HasForeignKey(t => t.StopId)
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

            modelBuilder.Entity<Ward>(entity =>
            {
                entity.ToTable("Wards");
            });

            modelBuilder.Entity<DepartureTime>(entity =>
            {
                entity.ToTable("DepartureTimes");
                // Khóa ngoại Route
                entity.HasOne(t => t.Route)
                    .WithMany(r => r.DepartureTimes)
                    .HasForeignKey(t => t.RouteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StopEstimate>(entity =>
            {
                entity.ToTable("StopEstimates");
                // Khóa ngoại Stop
                entity.HasOne(t => t.Stop)
                    .WithMany(r => r.StopEstimates)
                    .HasForeignKey(t => t.StopId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("Attendances");
                // Khóa ngoại TicketHistory
                entity.HasOne(t => t.HistoryTicket)
                    .WithMany(r => r.Attendances)
                    .HasForeignKey(t => t.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TicketType>(entity =>
            {
                entity.ToTable("TicketTypes");
                // Khóa ngoại Route
                entity.HasOne(t => t.Route)
                    .WithMany(r => r.TicketTypes)
                    .HasForeignKey(t => t.RouteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.ToTable("Trips");
                // Khóa ngoại Route
                entity.HasOne(t => t.Route)
                    .WithMany(r => r.Trips)
                    .HasForeignKey(t => t.RouteId)
                    .OnDelete(DeleteBehavior.Cascade);
                // Khóa ngoại Shuttle
                entity.HasOne(t => t.Shuttle)
                    .WithMany(r => r.Trips)
                    .HasForeignKey(t => t.ShuttleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("Promotions");
            });

            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.ToTable("Feedbacks");
                // Khóa ngoại User
                entity.HasOne(t => t.User)
                    .WithMany(r => r.Feedbacks)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SupportRequest>(entity =>
            {
                entity.ToTable("SupportRequests");
                // Khóa ngoại Route
                entity.HasOne(t => t.User)
                    .WithMany(r => r.SupportRequests)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShuttleLocationRecord>(entity =>
            {
                entity.ToTable("ShuttleLocationRecords");
                // Khóa ngoại Trip
                entity.HasOne(t => t.Trip)
                    .WithMany(r => r.ShuttleLocationRecords)
                    .HasForeignKey(t => t.TripId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TicketPromotion>(entity =>
            {
                entity.ToTable("TicketPromotions");
            });

            modelBuilder.Entity<TicketPromotion>()
                .HasKey(ur => new { ur.PromotionId, ur.TicketId });

            modelBuilder.Entity<TicketPromotion>()
                .HasOne(up => up.TicketType)
                .WithMany(u => u.TicketPromotions)
                .HasForeignKey(up => up.TicketId);

            modelBuilder.Entity<TicketPromotion>()
                .HasOne(up => up.Promotion)
                .WithMany(p => p.TicketPromotions)
                .HasForeignKey(up => up.PromotionId);

            modelBuilder.Entity<UserPromotion>(entity =>
            {
                entity.ToTable("UserPromotions");
            });

            modelBuilder.Entity<UserPromotion>()
                .HasKey(up => new { up.UserId, up.PromotionId });

            modelBuilder.Entity<UserPromotion>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPromotions)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserPromotion>()
                .HasOne(up => up.Promotion)
                .WithMany(p => p.UserPromotions)
                .HasForeignKey(up => up.PromotionId);

            modelBuilder.Entity<ChatBotLog>(entity =>
            {
                entity.ToTable("ChatBotLogs");
                // Khóa ngoại User
                entity.HasOne(t => t.User)
                    .WithMany(r => r.ChatBotLogs)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<HistoryTicket>(entity =>
            {
                entity.ToTable("HistoryTickets");
                // Khóa ngoại User
                entity.HasOne(t => t.User)
                    .WithMany(r => r.HistoryTickets)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                // Khóa ngoại Ticket
                entity.HasOne(t => t.TicketType)
                    .WithMany(r => r.HistoryTickets)
                    .HasForeignKey(t => t.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasOne(u => u.HistoryTicket)
                .WithOne(p => p.Transaction)
                .HasForeignKey<Transaction>(p => p.HistoryTicketId) // Khóa ngoại ở Transaction
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
            });

            modelBuilder.Entity<SystemLogs>(entity =>
            {
                entity.ToTable("SystemLogs");
                // Khóa ngoại User
                entity.HasOne(t => t.Actor)
                    .WithMany(r => r.SystemLogs)
                    .HasForeignKey(t => t.ActorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NotificationRecipient>(entity =>
            {
                entity.ToTable("NotificationRecipients");
                // Khóa ngoại Người nhận
                entity.HasOne(t => t.Recipient)
                    .WithMany(r => r.NotificationRecipients)
                    .HasForeignKey(t => t.RecipientId)
                    .OnDelete(DeleteBehavior.Cascade);
                // Khóa ngoại Noti
                entity.HasOne(t => t.Notification)
                    .WithMany(r => r.NotificationRecipients)
                    .HasForeignKey(t => t.NotificationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ResponseSupport>(entity =>
            {
                entity.ToTable("ResponseSupports");
                // Khóa ngoại sup request
                entity.HasOne(t => t.SupportRequest)
                    .WithMany(r => r.ResponseSupports)
                    .HasForeignKey(t => t.SupportRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NotificationTemplate>(entity =>
            {
                entity.ToTable("NotificationTemplates");
            });
        }
    }
}
