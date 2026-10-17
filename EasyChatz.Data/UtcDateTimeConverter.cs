using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EasyChatz.Data;

/// <summary>
/// Timestamps are written as UTC, but SQLite, SQL Server and MySQL hand them back with
/// <see cref="DateTimeKind.Unspecified"/>. Re-stamping on read means the UI's
/// <c>ToLocalTime()</c> is correct on every provider instead of silently off by the UTC offset.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
{
    public static UtcDateTimeConverter Instance { get; } = new();
}
