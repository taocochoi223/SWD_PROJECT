using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace SWD.DAL.Models;

public partial class IoTFinalDbContext : DbContext
{
    public IoTFinalDbContext()
    {
    }

    public IoTFinalDbContext(DbContextOptions<IoTFinalDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AlertHistory> AlertHistories { get; set; }

    public virtual DbSet<AlertRule> AlertRules { get; set; }

    public virtual DbSet<Hub> Hubs { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<Reading> Readings { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Sensor> Sensors { get; set; }

    public virtual DbSet<SensorType> SensorTypes { get; set; }

    public virtual DbSet<Site> Sites { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     optionsBuilder.UseSqlServer(GetConnectionString());
    // }

    // private string GetConnectionString()
    // {
    //     IConfiguration config = new ConfigurationBuilder()
    //          .SetBasePath(Directory.GetCurrentDirectory())
    //                 .AddJsonFile("appsettings.json", true, true)
    //                 .Build();
    //     var strConn = config["ConnectionStrings:DefaultConnection"];

    //     return strConn;
    // }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__AlertHis__4D7B4ADD112F38EB");

            entity.ToTable("AlertHistory");

            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.Message).HasMaxLength(255);
            entity.Property(e => e.ResolvedAt).HasColumnType("datetime");
            entity.Property(e => e.RuleId).HasColumnName("RuleID");
            entity.Property(e => e.SensorId).HasColumnName("SensorID");
            entity.Property(e => e.Severity).HasMaxLength(20);
            entity.Property(e => e.TriggeredAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Rule).WithMany(p => p.AlertHistories)
                .HasForeignKey(d => d.RuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AlertHist__RuleI__5AEE82B9");

            entity.HasOne(d => d.Sensor).WithMany(p => p.AlertHistories)
                .HasForeignKey(d => d.SensorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AlertHist__Senso__5BE2A6F2");
        });

        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.HasKey(e => e.RuleId).HasName("PK__AlertRul__110458C20C3B9D6E");

            entity.ToTable("AlertRule");

            entity.Property(e => e.RuleId).HasColumnName("RuleID");
            entity.Property(e => e.ConditionType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.NotificationMethod)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.SensorId).HasColumnName("SensorID");

            entity.HasOne(d => d.Sensor).WithMany(p => p.AlertRules)
                .HasForeignKey(d => d.SensorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AlertRule__Senso__571DF1D5");
        });

        modelBuilder.Entity<Hub>(entity =>
        {
            entity.HasKey(e => e.HubId).HasName("PK__Hub__9F4FFECFC0EB50EB");

            entity.ToTable("Hub");

            entity.HasIndex(e => e.MacAddress, "UQ__Hub__50EDF1CDFDF0BE28").IsUnique();

            entity.Property(e => e.HubId).HasColumnName("HubID");
            entity.Property(e => e.IsOnline).HasDefaultValue(false);
            entity.Property(e => e.LastHandshake).HasColumnType("datetime");
            entity.Property(e => e.MacAddress)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SiteId).HasColumnName("SiteID");

            entity.HasOne(d => d.Site).WithMany(p => p.Hubs)
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Hub__SiteID__3F466844");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotiId).HasName("PK__Notifica__EDC08EF20E2D3E48");

            entity.ToTable("Notification");

            entity.Property(e => e.NotiId).HasColumnName("NotiID");
            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.History).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.HistoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__Histo__60A75C0F");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__619B8048");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.OrgId).HasName("PK__Organiza__420C9E0C845179E3");

            entity.ToTable("Organization");

            entity.Property(e => e.OrgId).HasColumnName("OrgID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Reading>(entity =>
        {
            entity.HasKey(e => e.ReadingId).HasName("PK__Reading__C80F9C6E561FFF59");

            entity.ToTable("Reading");

            entity.HasIndex(e => new { e.SensorId, e.RecordedAt }, "IDX_Reading_Sensor_Time");

            entity.Property(e => e.ReadingId).HasColumnName("ReadingID");
            entity.Property(e => e.RecordedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SensorId).HasColumnName("SensorID");

            entity.HasOne(d => d.Sensor).WithMany(p => p.Readings)
                .HasForeignKey(d => d.SensorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reading__SensorI__49C3F6B7");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE3A10B7CB9B");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B6160395B11F5").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(e => e.SensorId).HasName("PK__Sensor__D809841AFEF09FD0");

            entity.ToTable("Sensor");

            entity.Property(e => e.SensorId).HasColumnName("SensorID");
            entity.Property(e => e.HubId).HasColumnName("HubID");
            entity.Property(e => e.LastUpdate).HasPrecision(3);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.TypeId).HasColumnName("TypeID");

            entity.HasOne(d => d.Hub).WithMany(p => p.Sensors)
                .HasForeignKey(d => d.HubId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sensor__HubID__44FF419A");

            entity.HasOne(d => d.Type).WithMany(p => p.Sensors)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sensor__TypeID__45F365D3");
        });

        modelBuilder.Entity<SensorType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK__SensorTy__516F0395C1AE3D64");

            entity.ToTable("SensorType");

            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.TypeName).HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(20);
        });

        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(e => e.SiteId).HasName("PK__Site__B9DCB9032CBAC039");

            entity.ToTable("Site");

            entity.Property(e => e.SiteId).HasColumnName("SiteID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.GeoLocation).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.OrgId).HasColumnName("OrgID");

            entity.HasOne(d => d.Org).WithMany(p => p.Sites)
                .HasForeignKey(d => d.OrgId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Site__OrgID__3A81B327");
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__SystemLo__5E5499A8547BC738");

            entity.ToTable("SystemLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Source)
                .HasMaxLength(50)
                .HasDefaultValue("EOH-Webhook");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CCACE687B793");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__A9D105342B2B253F").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OrgId).HasColumnName("OrgID");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.SiteId).HasColumnName("SiteID");

            entity.HasOne(d => d.Org).WithMany(p => p.Users)
                .HasForeignKey(d => d.OrgId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__OrgID__5165187F");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__RoleId__534D60F1");

            entity.HasOne(d => d.Site).WithMany(p => p.Users)
                .HasForeignKey(d => d.SiteId)
                .HasConstraintName("FK__User__SiteID__52593CB8");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
