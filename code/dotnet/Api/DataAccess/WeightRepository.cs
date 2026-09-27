using System.Globalization;
using Api.Controllers.Models;
using Microsoft.Data.Sqlite;

namespace Api.DataAccess
{
    public class WeightRepository
    {
        private readonly string _connectionString;

        public WeightRepository(string connectionString)
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
                CREATE TABLE IF NOT EXISTS Weights (
                    WeightId TEXT NOT NULL PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Weight TEXT NOT NULL,
                    Deleted INTEGER NOT NULL DEFAULT 0
                )
                """;
            command.ExecuteNonQuery();

            AddColumnIfMissing(connection, "Comment", "TEXT");
        }

        /// <summary>
        /// Adds a column to the Weights table if it isn't already present. SQLite's
        /// CREATE TABLE IF NOT EXISTS never alters an existing table, so this keeps a
        /// previously-deployed database in step with the schema without a migration
        /// framework (there are only two environments: local dev and one prod instance).
        /// </summary>
        private static void AddColumnIfMissing(SqliteConnection connection, string columnName, string columnType)
        {
            var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA table_info(Weights)";
            using (var reader = pragma.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE Weights ADD COLUMN {columnName} {columnType}";
            alter.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets all undeleted weight entries for a specific user.
        /// </summary>
        public async Task<IEnumerable<WeightEntity>> GetAllAsync(string userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT WeightId, UserId, Date, Weight, Deleted, Comment FROM Weights WHERE UserId = $userId AND Deleted = 0";
            command.Parameters.AddWithValue("$userId", userId);

            var results = new List<WeightEntity>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(ReadWeightEntity(reader));
            }
            return results;
        }

        /// <summary>
        /// Gets all weight entries.
        /// </summary>
        public async Task<IEnumerable<WeightEntity>> GetAllAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT WeightId, UserId, Date, Weight, Deleted, Comment FROM Weights";

            var results = new List<WeightEntity>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(ReadWeightEntity(reader));
            }
            return results;
        }

        public async Task<WeightEntity?> GetByIdAsync(Guid weightId, string userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT WeightId, UserId, Date, Weight, Deleted, Comment FROM Weights WHERE WeightId = $weightId AND UserId = $userId AND Deleted = 0";
            command.Parameters.AddWithValue("$weightId", weightId.ToString());
            command.Parameters.AddWithValue("$userId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadWeightEntity(reader);
            }
            return null;
        }

        public async Task AddAsync(WeightEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Weights (WeightId, UserId, Date, Weight, Deleted, Comment) VALUES ($weightId, $userId, $date, $weight, $deleted, $comment)";
            command.Parameters.AddWithValue("$weightId", entity.WeightId.ToString());
            command.Parameters.AddWithValue("$userId", entity.UserId);
            command.Parameters.AddWithValue("$date", ToUtc(entity.Date).ToString("O"));
            command.Parameters.AddWithValue("$weight", entity.Weight.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$deleted", entity.Deleted ? 1 : 0);
            command.Parameters.AddWithValue("$comment", (object?)NormaliseComment(entity.Comment) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(WeightEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Weights SET Date = $date, Weight = $weight, Deleted = $deleted, Comment = $comment WHERE WeightId = $weightId";
            command.Parameters.AddWithValue("$date", ToUtc(entity.Date).ToString("O"));
            command.Parameters.AddWithValue("$weight", entity.Weight.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$deleted", entity.Deleted ? 1 : 0);
            command.Parameters.AddWithValue("$comment", (object?)NormaliseComment(entity.Comment) ?? DBNull.Value);
            command.Parameters.AddWithValue("$weightId", entity.WeightId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        public async Task HardDeleteAsync(WeightEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Weights WHERE WeightId = $weightId";
            command.Parameters.AddWithValue("$weightId", entity.WeightId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(WeightEntity entity)
        {
            entity.Deleted = true;
            await UpdateAsync(entity);
        }

        private static WeightEntity ReadWeightEntity(SqliteDataReader reader)
        {
            return new WeightEntity(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                ToUtc(DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)),
                decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                reader.GetInt32(4) != 0,
                reader.IsDBNull(5) ? null : reader.GetString(5)
            );
        }

        /// <summary>
        /// Normalises a comment so that empty or whitespace-only text is stored as NULL
        /// rather than an empty string. This keeps display logic to a simple
        /// "show only when present" check and keeps the data clean.
        /// </summary>
        private static string? NormaliseComment(string? comment) =>
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        /// <summary>
        /// Normalises a date to UTC so weights are stored and returned as UTC regardless of the
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
