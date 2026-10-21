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

    public BackupController(UserRepository userRepository, DoseRepository doseRepository)
    {
        _userRepository = userRepository;
        _doseRepository = doseRepository;
    }

    [HttpGet]
    public async Task<Backup> GetBackup()
    {
        var users = (await _userRepository.GetAllAsync())
            .Select(entity => new User(entity.UserId, entity.HeightInCm));

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
            await _userRepository.AddAsync(new UserEntity(user.UserId, user.heightInCm));
        }
        foreach (var doseRecord in backup.DoseRecords)
        {
            await _doseRepository.AddAsync(new DoseEntity(
                doseRecord.DoseId == Guid.Empty ? Guid.NewGuid() : doseRecord.DoseId,
                doseRecord.UserId,
                doseRecord.TakenAt,
                doseRecord.MedicationName,
                doseRecord.DoseAmountMg,
                doseRecord.Deleted,
                doseRecord.Notes));
        }

        return Ok();
    }
}
