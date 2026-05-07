using Microsoft.Data.Sqlite;

namespace OpenSpartan.Workshop.Data
{
    // Single source of truth for "read column or fall back if NULL". Centralized
    // because the previous inline pattern of repeating
    //     reader.IsDBNull(xOrdinal) ? fallback : reader.GetFieldValue<T>(yOrdinal)
    // for every column hid two real bugs (EndTime / LastTeamId checking the wrong
    // ordinal) inside ~40 lines of look-alike boilerplate.
    internal static class SqliteReaderExtensions
    {
        // For non-nullable columns with a sentinel fallback (string.Empty, 0, enum default).
        internal static T GetOrDefault<T>(this SqliteDataReader reader, int ordinal, T fallback) =>
            reader.IsDBNull(ordinal) ? fallback : reader.GetFieldValue<T>(ordinal);

        // For nullable value-type columns. SqliteDataReader.GetFieldValue<int?> doesn't work,
        // so this helper unboxes from the non-nullable read.
        internal static T? GetNullable<T>(this SqliteDataReader reader, int ordinal) where T : struct =>
            reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<T>(ordinal);

        // For nullable string columns.
        internal static string? GetStringOrNull(this SqliteDataReader reader, int ordinal) =>
            reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<string>(ordinal);
    }
}
