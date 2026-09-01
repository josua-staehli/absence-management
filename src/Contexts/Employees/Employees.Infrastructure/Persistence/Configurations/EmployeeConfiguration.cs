using Employees.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employees.Infrastructure.Persistence.Configurations;

/// <summary>
///     Maps the employee aggregate to its table. The column limits live here and not in the
///     aggregate: they are a storage decision, not a business rule.
/// </summary>
internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    private const int MaxNameLength = 100;
    private const int MaxEmailLength = 256;

    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.FirstName)
            .HasMaxLength(MaxNameLength)
            .IsRequired();

        builder.Property(employee => employee.LastName)
            .HasMaxLength(MaxNameLength)
            .IsRequired();

        builder.Property(employee => employee.Email)
            .HasMaxLength(MaxEmailLength)
            .IsRequired();

        // One work address per employee. The index compares the stored text case-sensitively, so
        // it rejects exact duplicates only; case-insensitive uniqueness would need the aggregate
        // to normalize the address first.
        builder.HasIndex(employee => employee.Email).IsUnique();
    }
}
