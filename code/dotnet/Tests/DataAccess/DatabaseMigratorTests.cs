using Api.DataAccess;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;

namespace Tests.DataAccess;

public class DatabaseMigratorTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"migrator-tests-{Guid.NewGuid():N}.db");
    private string ConnectionString => $"Data Source={_dbPath};Pooling=False";

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public void Migrate_OnANewDatabase_CreatesTheTablesAndRecordsTheScripts()
    {
        DatabaseMigrator.Migrate(ConnectionString);

        TableNames().Should().Contain(new[] { "Doses", "Users", "SchemaVersions" });
        AppliedScripts().Should().ContainSingle(s => s.EndsWith("0001_InitialSchema.sql"));
    }

    [Fact]
    public void Migrate_RunTwice_AppliesEachScriptOnce()
    {
        DatabaseMigrator.Migrate(ConnectionString);
        var afterFirst = AppliedScripts();

        DatabaseMigrator.Migrate(ConnectionString);

        AppliedScripts().Should().Equal(afterFirst);
    }

    [Fact]
    public void Migrate_OnADatabaseCreatedBeforeDbUp_KeepsItsData()
    {
        // The repositories used to create their own tables on startup, so existing
        // databases have the tables and data but no SchemaVersions.
        Execute("""
            CREATE TABLE Doses (
                DoseId TEXT NOT NULL PRIMARY KEY,
                UserId TEXT NOT NULL,
                TakenAt TEXT NOT NULL,
                MedicationName TEXT NOT NULL,
                DoseAmountMg TEXT NOT NULL,
                Deleted INTEGER NOT NULL DEFAULT 0,
                Notes TEXT
            );
            CREATE TABLE Users (UserId TEXT NOT NULL PRIMARY KEY);
            INSERT INTO Users (UserId) VALUES ('alice');
            INSERT INTO Doses (DoseId, UserId, TakenAt, MedicationName, DoseAmountMg)
                VALUES ('d1', 'alice', '2025-01-01T00:00:00.0000000Z', 'Semaglutide', '1.0');
            """);

        DatabaseMigrator.Migrate(ConnectionString);

        Scalar("SELECT COUNT(*) FROM Users").Should().Be(1);
        Scalar("SELECT COUNT(*) FROM Doses").Should().Be(1);
        AppliedScripts().Should().ContainSingle(s => s.EndsWith("0001_InitialSchema.sql"));
    }

    private List<string> TableNames() =>
        Strings("SELECT name FROM sqlite_master WHERE type = 'table'");

    private List<string> AppliedScripts() =>
        Strings("SELECT ScriptName FROM SchemaVersions ORDER BY ScriptName");

    private List<string> Strings(string sql)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        var results = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) results.Add(reader.GetString(0));
        return results;
    }

    private long Scalar(string sql)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private void Execute(string sql)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
