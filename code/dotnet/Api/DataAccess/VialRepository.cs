using System.Globalization;
using Api.Controllers.Models;
using Microsoft.Data.Sqlite;

namespace Api.DataAccess;

/// <summary>Each user's current vial of each medication (at most one per user and medication).</summary>
public class VialRepository
{
    // Joins Medications so each vial comes back with its medication's name.
    private const string SelectVials =
        "SELECT v.VialId, v.UserId, v.MedicationId, m.Name, v.InitialAmountMg, v.InitialVolumeMl, v.StartedOn " +
        "FROM Vials v JOIN Medications m ON m.MedicationId = v.MedicationId";

    // StartedOn is a calendar date, stored without a time or timezone.
    private const string DateFormat = "yyyy-MM-dd";

    private readonly string _connectionString;

    public VialRepository(string connectionString)
    {
        _connectionString = ConnectionStrings.WithForeignKeys(connectionString);
    }

    /// <summary>A user's vials, in medication display order.</summary>
    public async Task<IReadOnlyList<VialEntity>> GetAllAsync(string userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = $"{SelectVials} WHERE v.UserId = $userId ORDER BY m.DisplayOrder";
        command.Parameters.AddWithValue("$userId", userId);
        return await ReadAllAsync(command);
    }

    /// <summary>Every user's vials (for backup).</summary>
    public async Task<IReadOnlyList<VialEntity>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = $"{SelectVials} ORDER BY v.UserId, m.DisplayOrder";
        return await ReadAllAsync(command);
    }

    /// <summary>
    /// Saves the user's vial of the entity's medication: inserts it if the user has none yet,
    /// otherwise replaces the amounts and start date of the existing one (which keeps its VialId).
    /// </summary>
    public async Task UpsertAsync(VialEntity entity)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO Vials (VialId, UserId, MedicationId, InitialAmountMg, InitialVolumeMl, StartedOn) " +
            "VALUES ($vialId, $userId, $medicationId, $initialAmountMg, $initialVolumeMl, $startedOn) " +
            "ON CONFLICT (UserId, MedicationId) DO UPDATE SET " +
            "InitialAmountMg = excluded.InitialAmountMg, InitialVolumeMl = excluded.InitialVolumeMl, " +
            "StartedOn = excluded.StartedOn";
        command.Parameters.AddWithValue("$vialId", entity.VialId.ToString());
        command.Parameters.AddWithValue("$userId", entity.UserId);
        command.Parameters.AddWithValue("$medicationId", entity.MedicationId);
        command.Parameters.AddWithValue("$initialAmountMg", entity.InitialAmountMg.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$initialVolumeMl", entity.InitialVolumeMl.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$startedOn", entity.StartedOn.ToString(DateFormat, CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync();
    }

    public async Task HardDeleteAsync(VialEntity entity)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Vials WHERE VialId = $vialId";
        command.Parameters.AddWithValue("$vialId", entity.VialId.ToString());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<VialEntity>> ReadAllAsync(SqliteCommand command)
    {
        var results = new List<VialEntity>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new VialEntity(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetInt32(2),
                decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                DateOnly.ParseExact(reader.GetString(6), DateFormat, CultureInfo.InvariantCulture))
            {
                MedicationName = reader.GetString(3),
            });
        }
        return results;
    }
}
