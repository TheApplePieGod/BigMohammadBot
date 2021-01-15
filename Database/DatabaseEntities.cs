using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BigMohammadBot.Database
{
    public partial class DatabaseEntities : DbContext
    {
        public DatabaseEntities()
        {
        }

        public DatabaseEntities(DbContextOptions<DatabaseEntities> options)
            : base(options)
        {
        }

        public virtual DbSet<ActivityLog> ActivityLog { get; set; }
        public virtual DbSet<ActivityTypes> ActivityTypes { get; set; }
        public virtual DbSet<AppState> AppState { get; set; }
        public virtual DbSet<ChannelBlacklist> ChannelBlacklist { get; set; }
        public virtual DbSet<ChannelTypes> ChannelTypes { get; set; }
        public virtual DbSet<Channels> Channels { get; set; }
        public virtual DbSet<Greetings> Greetings { get; set; }
        public virtual DbSet<MessageStatistics> MessageStatistics { get; set; }
        public virtual DbSet<SupressedUsers> SupressedUsers { get; set; }
        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<VoiceStatistics> VoiceStatistics { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=BigMohammadBot;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.Property(e => e.Information).IsUnicode(false);

                entity.Property(e => e.ResultText).IsUnicode(false);
            });

            modelBuilder.Entity<ActivityTypes>(entity =>
            {
                entity.Property(e => e.Name).IsUnicode(false);
            });

            modelBuilder.Entity<AppState>(entity =>
            {
                entity.Property(e => e.HelloDeleted).HasDefaultValueSql("((1))");

                entity.Property(e => e.HelloTopic).HasDefaultValueSql("('')");

                entity.Property(e => e.JoinMuteMinutes).HasDefaultValueSql("((30))");

                entity.Property(e => e.StatisticsPeriodStart).HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<ChannelTypes>(entity =>
            {
                entity.Property(e => e.Name).IsUnicode(false);
            });

            modelBuilder.Entity<Channels>(entity =>
            {
                entity.Property(e => e.DiscordChannelName).IsUnicode(false);
            });

            modelBuilder.Entity<Greetings>(entity =>
            {
                entity.Property(e => e.Greeting).IsUnicode(false);
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.DiscordUserName).IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
