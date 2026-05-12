using M2.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace M2.Infrastructure.Persistence.Configurations;

/// <summary>
/// Extension methods for mapping the <see cref="BilingualText"/> value object (ADR-022).
/// Columns are named <c>{propertyName}_en</c> and <c>{propertyName}_zht</c>.
/// Both languages are always required — never store only one (ADR-022).
/// </summary>
public static class BilingualTextConfiguration
{
    /// <summary>
    /// Maps a <see cref="BilingualText"/> navigation property as an owned entity
    /// with columns <c>{propertyName}_en</c> and <c>{propertyName}_zht</c>.
    /// </summary>
    public static void OwnsOneBilingual<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, BilingualText?>> navigationExpression)
        where TEntity : class
    {
        var propertyName = GetPropertyName(navigationExpression);

        builder.OwnsOne(navigationExpression, owned =>
        {
            owned.Property(b => b.En)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName($"{propertyName}_en");

            owned.Property(b => b.Zht)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName($"{propertyName}_zht");
        });
    }

    private static string GetPropertyName<TEntity, TProperty>(
        Expression<Func<TEntity, TProperty>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;

        throw new ArgumentException(
            $"Expression '{expression}' does not refer to a property.", nameof(expression));
    }
}
