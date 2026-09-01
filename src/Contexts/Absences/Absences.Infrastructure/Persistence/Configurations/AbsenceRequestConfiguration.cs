using Absences.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Absences.Infrastructure.Persistence.Configurations;

/// <summary>
///     Maps the absence request aggregate to its table. A column limit that is only a storage
///     decision is declared here; the length of the comment is a rule of the aggregate, so this
///     configuration reads it from <see cref="AbsenceRequest.MaxCommentLength" /> instead of
///     repeating the number.
/// </summary>
internal sealed class AbsenceRequestConfiguration : IEntityTypeConfiguration<AbsenceRequest>
{
    private const int MaxEnumLength = 20;

    public void Configure(EntityTypeBuilder<AbsenceRequest> builder)
    {
        builder.ToTable("absence_requests");

        builder.HasKey(absenceRequest => absenceRequest.Id);

        builder.Property(absenceRequest => absenceRequest.EmployeeId).IsRequired();

        // Enums are stored as strings: readable in the database and stable if a value is ever
        // reordered.
        builder.Property(absenceRequest => absenceRequest.Type)
            .HasConversion<string>()
            .HasMaxLength(MaxEnumLength)
            .IsRequired();

        builder.Property(absenceRequest => absenceRequest.Status)
            .HasConversion<string>()
            .HasMaxLength(MaxEnumLength)
            .IsRequired();

        // The DateRange value object has no identity of its own, so it is mapped into two columns
        // of the same table instead of a table of its own.
        builder.OwnsOne(absenceRequest => absenceRequest.Period, period =>
        {
            period.Property(range => range.Start).HasColumnName("StartDate").IsRequired();
            period.Property(range => range.End).HasColumnName("EndDate").IsRequired();
        });
        builder.Navigation(absenceRequest => absenceRequest.Period).IsRequired();

        builder.Property(absenceRequest => absenceRequest.Comment)
            .HasMaxLength(AbsenceRequest.MaxCommentLength);

        builder.Property(absenceRequest => absenceRequest.CreatedAt).IsRequired();

        // No foreign key: the employee lives in another bounded context's database, so the
        // constraint cannot exist even in principle. That the id belongs to an employee is
        // established once, when the request is created, by asking IEmployeeDirectory. The index
        // stays, because the overlap check filters by employee on every write.
        builder.HasIndex(absenceRequest => absenceRequest.EmployeeId);

        // A shortcut over Status, not a column of its own.
        builder.Ignore(absenceRequest => absenceRequest.IsOpen);
    }
}
