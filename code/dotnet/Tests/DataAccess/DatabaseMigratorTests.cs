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

        TableNames().Should().Contain(new[] { "Doses", "Users", "Medications", "Vials", "SchemaVersions" });
        AppliedScripts().Select(Path.GetFileNameWithoutExtension).Should().Equal(
            "Api.Migrations.0001_InitialSchema",
            "Api.Migrations.0002_AddMedications",
            "Api.Migrations.0003_DosesReferenceMedicationsById",
            "Api.Migrations.0004_AddVials");
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
    public void Migrate_OnADatabaseCreatedBeforeDbUp_KeepsItsData_AndLinksDosesByMedicationId()
    {
        CreatePreDbUpDatabase("""
            INSERT INTO Doses (DoseId, UserId, TakenAt, MedicationName, DoseAmountMg, Notes) VALUES
                ('d1', 'alice', '2025-01-01T00:00:00.0000000Z', 'Semaglutide', '1.0', 'first'),
                ('d2', 'alice', '2025-01-02T00:00:00.0000000Z', 'tirzepatide', '2.5', NULL),
                ('d3', 'alice', '2025-01-03T00:00:00.0000000Z', 'RETATRUTIDE', '4', NULL);
            """);

        DatabaseMigrator.Migrate(ConnectionString);

        Scalar("SELECT COUNT(*) FROM Users").Should().Be(1);
        Strings("SELECT DoseId || ':' || MedicationId || ':' || DoseAmountMg || ':' || IFNULL(Notes, '') FROM Doses ORDER BY DoseId")
            .Should().Equal("d1:1:1.0:first", "d2:2:2.5:", "d3:3:4:");
        Strings("SELECT name FROM pragma_table_info('Doses')").Should().NotContain("MedicationName");
    }

    [Fact]
    public void Migrate_WithADoseForAnUnknownMedication_FailsAndLeavesTheDosesAsTheyWere()
    {
        CreatePreDbUpDatabase("""
            INSERT INTO Doses (DoseId, UserId, TakenAt, MedicationName, DoseAmountMg) VALUES
                ('d1', 'alice', '2025-01-01T00:00:00.0000000Z', 'Semaglutide', '1.0'),
                ('d2', 'alice', '2025-01-02T00:00:00.0000000Z', 'Ozempic', '1.0');
            """);

        var act = () => DatabaseMigrator.Migrate(ConnectionString);

        act.Should().Throw<InvalidOperationException>().WithMessage("*0003_DosesReferenceMedicationsById*");
        Strings("SELECT MedicationName FROM Doses ORDER BY DoseId").Should().Equal("Semaglutide", "Ozempic");
        AppliedScripts().Should().NotContain(s => s.Contains("0003"));
    }

    /// <summary>
    /// The shape databases had before DbUp, when the repositories created their own tables on
    /// startup: the tables and data, but no SchemaVersions.
    /// </summary>
    private void CreatePreDbUpDatabase(string insertDoses) => Execute($"""
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
        {insertDoses}
        """);

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
