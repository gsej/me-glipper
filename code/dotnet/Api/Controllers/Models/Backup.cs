namespace Api.Controllers.Models;

/// <summary>A full-database snapshot: all users and all weight records (including soft-deleted).</summary>
public record Backup(IEnumerable<User>? Users, IEnumerable<WeightRecord>? WeightRecords);
