using Microsoft.Data.Sqlite;

namespace Api.DataAccess;

public static class ConnectionStrings
{
    /// <summary>
    /// Turns on foreign-key enforcement, which SQLite leaves off for every connection unless
    /// asked. Applied by every repository and the migrator, so the Doses -> Medications
    /// reference is always enforced.
    /// </summary>
    public static string WithForeignKeys(string connectionString) =>
        new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true }.ToString();
}
