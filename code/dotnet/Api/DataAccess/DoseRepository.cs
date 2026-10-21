using System.Globalization;
using Api.Controllers.Models;
using Microsoft.Data.Sqlite;

namespace Api.DataAccess
{
    public class DoseRepository
    {
        private readonly string _connectionString;

        public DoseRepository(string connectionString)
        {
            _connectionString = connectionString;
            EnsureTablesExist();
        }

        private void EnsureTablesExist()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS Doses (
                    DoseId TEXT NOT NULL PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    TakenAt TEXT NOT NULL,
                    MedicationName TEXT NOT NULL,
                    DoseAmountMg TEXT NOT NULL,
                    Deleted INTEGER NOT NULL DEFAULT 0,
                    Notes TEXT
                )
                """;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets all undeleted dose entries for a specific user.
        /// </summary>
        public async Task<IEnumerable<DoseEntity>> GetAllAsync(string userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT DoseId, UserId, TakenAt, MedicationName, DoseAmountMg, Deleted, Notes FROM Doses WHERE UserId = $userId AND Deleted = 0";
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
            command.CommandText = "SELECT DoseId, UserId, TakenAt, MedicationName, DoseAmountMg, Deleted, Notes FROM Doses";

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
            command.CommandText = "SELECT DoseId, UserId, TakenAt, MedicationName, DoseAmountMg, Deleted, Notes FROM Doses WHERE DoseId = $doseId AND UserId = $userId AND Deleted = 0";
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
            command.CommandText = "INSERT INTO Doses (DoseId, UserId, TakenAt, MedicationName, DoseAmountMg, Deleted, Notes) VALUES ($doseId, $userId, $takenAt, $medicationName, $doseAmountMg, $deleted, $notes)";
            command.Parameters.AddWithValue("$doseId", entity.DoseId.ToString());
            command.Parameters.AddWithValue("$userId", entity.UserId);
            command.Parameters.AddWithValue("$takenAt", ToUtc(entity.TakenAt).ToString("O"));
            command.Parameters.AddWithValue("$medicationName", entity.MedicationName);
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
            command.CommandText = "UPDATE Doses SET TakenAt = $takenAt, MedicationName = $medicationName, DoseAmountMg = $doseAmountMg, Deleted = $deleted, Notes = $notes WHERE DoseId = $doseId";
            command.Parameters.AddWithValue("$takenAt", ToUtc(entity.TakenAt).ToString("O"));
            command.Parameters.AddWithValue("$medicationName", entity.MedicationName);
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
                reader.GetString(3),
                decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                reader.GetInt32(5) != 0,
                reader.IsDBNull(6) ? null : reader.GetString(6)
            );
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
