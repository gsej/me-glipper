namespace Api.Controllers.Models;

/// <summary>A full-database snapshot: all users and all dose records (including soft-deleted).</summary>
public record Backup(IEnumerable<User>? Users, IEnumerable<DoseRecord>? DoseRecords);
