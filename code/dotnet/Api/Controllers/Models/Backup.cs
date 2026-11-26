namespace Api.Controllers.Models;

/// <summary>
/// A full-database snapshot: all users, all dose records (including soft-deleted) and all vials.
/// Vials are optional on restore, so backups taken before vials existed still restore (with none).
/// </summary>
public record Backup(IEnumerable<User>? Users, IEnumerable<DoseRecord>? DoseRecords, IEnumerable<VialRecord>? Vials = null);
