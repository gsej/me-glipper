using DbUp;

namespace Api.DataAccess;

/// <summary>
/// Brings the SQLite database up to the current schema by running the SQL scripts embedded
/// from <c>Migrations/</c>, in filename order, each in its own transaction. Applied scripts
/// are recorded in DbUp's <c>SchemaVersions</c> table, so each runs once per database.
/// Never edit a script once it has been deployed; add a new one instead.
/// </summary>
public static class DatabaseMigrator
{
    public static void Migrate(string connectionString)
    {
        var upgrader = DeployChanges.To
            .SqliteDatabase(ConnectionStrings.WithForeignKeys(connectionString))
            .WithScriptsEmbeddedInAssembly(
                typeof(DatabaseMigrator).Assembly,
                name => name.Contains(".Migrations.") && name.EndsWith(".sql"))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            // Fail startup rather than serve requests against a half-migrated database.
            throw new InvalidOperationException(
                $"Database migration failed on script '{result.ErrorScript?.Name}'.", result.Error);
        }
    }
}
