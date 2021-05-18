using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

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

        public virtual DbSet<ActivityLog> ActivityLogs { get; set; }
        public virtual DbSet<ActivityType> ActivityTypes { get; set; }
        public virtual DbSet<AppState> AppStates { get; set; }
        public virtual DbSet<Channel> Channels { get; set; }
        public virtual DbSet<ChannelBlacklist> ChannelBlacklists { get; set; }
        public virtual DbSet<ChannelType> ChannelTypes { get; set; }
        public virtual DbSet<Emote> Emotes { get; set; }
        public virtual DbSet<Greeting> Greetings { get; set; }
        public virtual DbSet<MessageStatistic> MessageStatistics { get; set; }
        public virtual DbSet<SchemaVersion> SchemaVersions { get; set; }
        public virtual DbSet<SupressedUser> SupressedUsers { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<VoiceStatistic> VoiceStatistics { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=BigMohammadBot;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.Property(e => e.Information).IsUnicode(false);

                entity.Property(e => e.ResultText).IsUnicode(false);
            });

            modelBuilder.Entity<ActivityType>(entity =>
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

            modelBuilder.Entity<Channel>(entity =>
            {
                entity.Property(e => e.DiscordChannelName).IsUnicode(false);
            });

            modelBuilder.Entity<ChannelType>(entity =>
            {
                entity.Property(e => e.Name).IsUnicode(false);
            });

            modelBuilder.Entity<Emote>(entity =>
            {
                entity.Property(e => e.Link).IsUnicode(false);

                entity.Property(e => e.Name).IsUnicode(false);
            });

            modelBuilder.Entity<Greeting>(entity =>
            {
                entity.Property(e => e.Greeting1).IsUnicode(false);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.DiscordUserName).IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
