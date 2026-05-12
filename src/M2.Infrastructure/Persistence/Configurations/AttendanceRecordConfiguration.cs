using M2.Domain.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2.Infrastructure.Persistence.Configurations;

public sealed class AttendanceRecordConfiguration : BaseEntityConfiguration<AttendanceRecord>
{
    public override void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records");
        builder.HasKey(ar => ar.Id);
        builder.Property(ar => ar.Id).HasColumnType("uuid").ValueGeneratedNever();

        base.Configure(builder);

        builder.Property(ar => ar.EmployeeId)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("EmployeeId");

        builder.Property(ar => ar.ClockInAt)
            .IsRequired()
            .HasColumnName("ClockInAt")
            .HasColumnType("timestamptz");

        builder.Property(ar => ar.ClockOutAt)
            .HasColumnName("ClockOutAt")
            .HasColumnType("timestamptz");

        builder.Property(ar => ar.Source)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("Source")
            .HasConversion<string>();

        builder.Property(ar => ar.Notes)
            .HasColumnType("text")
            .HasColumnName("Notes");

        builder.HasIndex(ar => new { ar.TenantId, ar.EmployeeId, ar.ClockInAt })
            .HasDatabaseName("IX_attendance_records_TenantId_EmployeeId_ClockInAt");

        builder.HasQueryFilter(ar => !ar.IsDeleted);
    }
}
