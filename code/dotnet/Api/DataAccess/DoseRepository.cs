using System.Globalization;
using Api.Controllers.Models;
using Microsoft.Data.Sqlite;

namespace Api.DataAccess
{
    public class DoseRepository
    {
        // Joins Medications so each dose comes back with its medication's name.
        private const string SelectDoses =
            "SELECT d.DoseId, d.UserId, d.TakenAt, d.MedicationId, m.Name, d.DoseAmountMg, d.Deleted, d.Notes " +
            "FROM Doses d JOIN Medications m ON m.MedicationId = d.MedicationId";

        private readonly string _connectionString;

        public DoseRepository(string connectionString)
        {
            _connectionString = ConnectionStrings.WithForeignKeys(connectionString);
        }

        /// <summary>
        /// Gets all undeleted dose entries for a specific user.
        /// </summary>
        public async Task<IEnumerable<DoseEntity>> GetAllAsync(string userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = $"{SelectDoses} WHERE d.UserId = $userId AND d.Deleted = 0";
            command.Parameters.AddWithValue("$userId", userId);

            var results = new List<DoseEntity>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(ReadDoseEntity(reader));
            }
            return results;
        }

        /// <summary>
        /// Gets all dose entries.
        /// </summary>
        public async Task<IEnumerable<DoseEntity>> GetAllAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = SelectDoses;

            var results = new List<DoseEntity>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(ReadDoseEntity(reader));
            }
            return results;
        }

        public async Task<DoseEntity?> GetByIdAsync(Guid doseId, string userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = $"{SelectDoses} WHERE d.DoseId = $doseId AND d.UserId = $userId AND d.Deleted = 0";
            command.Parameters.AddWithValue("$doseId", doseId.ToString());
            command.Parameters.AddWithValue("$userId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadDoseEntity(reader);
            }
            return null;
        }

        public async Task AddAsync(DoseEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Doses (DoseId, UserId, TakenAt, MedicationId, DoseAmountMg, Deleted, Notes) VALUES ($doseId, $userId, $takenAt, $medicationId, $doseAmountMg, $deleted, $notes)";
            command.Parameters.AddWithValue("$doseId", entity.DoseId.ToString());
            command.Parameters.AddWithValue("$userId", entity.UserId);
            command.Parameters.AddWithValue("$takenAt", ToUtc(entity.TakenAt).ToString("O"));
            command.Parameters.AddWithValue("$medicationId", entity.MedicationId);
            command.Parameters.AddWithValue("$doseAmountMg", entity.DoseAmountMg.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$deleted", entity.Deleted ? 1 : 0);
            command.Parameters.AddWithValue("$notes", (object?)NormaliseNotes(entity.Notes) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(DoseEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Doses SET TakenAt = $takenAt, MedicationId = $medicationId, DoseAmountMg = $doseAmountMg, Deleted = $deleted, Notes = $notes WHERE DoseId = $doseId";
            command.Parameters.AddWithValue("$takenAt", ToUtc(entity.TakenAt).ToString("O"));
            command.Parameters.AddWithValue("$medicationId", entity.MedicationId);
            command.Parameters.AddWithValue("$doseAmountMg", entity.DoseAmountMg.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$deleted", entity.Deleted ? 1 : 0);
            command.Parameters.AddWithValue("$notes", (object?)NormaliseNotes(entity.Notes) ?? DBNull.Value);
            command.Parameters.AddWithValue("$doseId", entity.DoseId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        public async Task HardDeleteAsync(DoseEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Doses WHERE DoseId = $doseId";
            command.Parameters.AddWithValue("$doseId", entity.DoseId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(DoseEntity entity)
        {
            entity.Deleted = true;
            await UpdateAsync(entity);
        }

        private static DoseEntity ReadDoseEntity(SqliteDataReader reader)
        {
            return new DoseEntity(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                ToUtc(DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)),
                reader.GetInt32(3),
                decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                reader.GetInt32(6) != 0,
                reader.IsDBNull(7) ? null : reader.GetString(7)
            )
            {
                MedicationName = reader.GetString(4),
            };
        }

        /// <summary>
        /// Normalises notes so that empty or whitespace-only text is stored as NULL
        /// rather than an empty string. This keeps display logic to a simple
        /// "show only when present" check and keeps the data clean.
        /// </summary>
        private static string? NormaliseNotes(string? notes) =>
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        /// <summary>
        /// Normalises a date to UTC so doses are stored and returned as UTC regardless of the
        /// server's timezone. A Local value is converted; an Unspecified value is assumed to
        /// already be UTC (relabelled without shifting the clock).
        /// </summary>
        private static DateTime ToUtc(DateTime date) => date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }
}
