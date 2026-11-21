using Api.Controllers.Models;
using Api.DataAccess;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/backup")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class BackupController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly DoseRepository _doseRepository;
    private readonly MedicationRepository _medicationRepository;

    public BackupController(
        UserRepository userRepository,
        DoseRepository doseRepository,
        MedicationRepository medicationRepository)
    {
        _userRepository = userRepository;
        _doseRepository = doseRepository;
        _medicationRepository = medicationRepository;
    }

    [HttpGet]
    public async Task<Backup> GetBackup()
    {
        var users = (await _userRepository.GetAllAsync())
            .Select(entity => new User(entity.UserId));

        var doseRecords = (await _doseRepository.GetAllAsync())
            .Select(entity => new DoseRecord(
                entity.DoseId,
                entity.UserId,
                entity.TakenAt,
                entity.MedicationName,
                entity.DoseAmountMg,
                entity.Deleted,
                entity.Notes))
            .OrderBy(record => record.TakenAt);

        return new Backup(users, doseRecords);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore([FromBody] Backup? backup)
    {
        // A restore is a full snapshot, so both sections are required.
        if (backup?.Users == null || backup.DoseRecords == null)
            return BadRequest("No records provided");

        // Backups name each dose's medication; resolve the names to ids before wiping
        // anything, so a backup with an unknown medication leaves the database untouched.
        var medicationIds = (await _medicationRepository.GetAllAsync())
            .ToDictionary(m => m.Name, m => m.MedicationId, StringComparer.OrdinalIgnoreCase);
        var unknown = backup.DoseRecords
            .Select(d => d.MedicationName)
            .Where(name => !medicationIds.ContainsKey(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknown.Count > 0)
            return BadRequest($"Unknown medication(s): {string.Join(", ", unknown)}.");

        // Wipe everything, then re-insert users before doses.
        foreach (var entity in await _doseRepository.GetAllAsync())
        {
            await _doseRepository.HardDeleteAsync(entity);
        }
        foreach (var entity in await _userRepository.GetAllAsync())
        {
            await _userRepository.HardDeleteAsync(entity);
        }

        foreach (var user in backup.Users)
        {
            await _userRepository.AddAsync(new UserEntity(user.UserId));
        }
        foreach (var doseRecord in backup.DoseRecords)
        {
            await _doseRepository.AddAsync(new DoseEntity(
                doseRecord.DoseId == Guid.Empty ? Guid.NewGuid() : doseRecord.DoseId,
                doseRecord.UserId,
                doseRecord.TakenAt,
                medicationIds[doseRecord.MedicationName],
                doseRecord.DoseAmountMg,
                doseRecord.Deleted,
                doseRecord.Notes));
        }

        return Ok();
    }
}
