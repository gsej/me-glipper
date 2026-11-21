using Api.Controllers.Models;
using Microsoft.Data.Sqlite;

namespace Api.DataAccess;

/// <summary>Read-only access to the Medications reference table (seeded by migrations).</summary>
public class MedicationRepository
{
    private const string SelectColumns =
        "SELECT MedicationId, Name, DefaultDosingIntervalDays, HalfLifeHours, Bioavailability, TmaxHours, ChartColor, DisplayOrder FROM Medications";

    private readonly string _connectionString;

    public MedicationRepository(string connectionString)
    {
        _connectionString = ConnectionStrings.WithForeignKeys(connectionString);
    }

    /// <summary>All medications, in display order.</summary>
    public async Task<IReadOnlyList<MedicationEntity>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} ORDER BY DisplayOrder";

        var results = new List<MedicationEntity>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(Read(reader));
        }
        return results;
    }

    /// <summary>The medication with this name, ignoring case, or null if there is none.</summary>
    public async Task<MedicationEntity?> GetByNameAsync(string name)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        // Name is COLLATE NOCASE, so this comparison ignores case.
        command.CommandText = $"{SelectColumns} WHERE Name = $name";
        command.Parameters.AddWithValue("$name", name);

        using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Read(reader) : null;
    }

    private static MedicationEntity Read(SqliteDataReader reader) => new(
        reader.GetInt32(0),
        reader.GetString(1),
        reader.GetDouble(2),
        reader.GetDouble(3),
        reader.GetDouble(4),
        reader.GetDouble(5),
        reader.GetString(6),
        reader.GetInt32(7));
}
