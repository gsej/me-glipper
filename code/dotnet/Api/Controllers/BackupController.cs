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
    private readonly WeightRepository _weightRepository;

    public BackupController(UserRepository userRepository, WeightRepository weightRepository)
    {
        _userRepository = userRepository;
        _weightRepository = weightRepository;
    }

    [HttpGet]
    public async Task<Backup> GetBackup()
    {
        var users = (await _userRepository.GetAllAsync())
            .Select(entity => new User(entity.UserId, entity.HeightInCm));

        var weightRecords = (await _weightRepository.GetAllAsync())
            .Select(entity => new WeightRecord(
                entity.WeightId,
                entity.UserId,
                entity.Date,
                entity.Weight,
                entity.Deleted,
                entity.Comment))
            .OrderBy(record => record.Date);

        return new Backup(users, weightRecords);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore([FromBody] Backup? backup)
    {
        // A restore is a full snapshot, so both sections are required.
        if (backup?.Users == null || backup.WeightRecords == null)
            return BadRequest("No records provided");

        // Wipe everything, then re-insert users before weights.
        foreach (var entity in await _weightRepository.GetAllAsync())
        {
            await _weightRepository.HardDeleteAsync(entity);
        }
        foreach (var entity in await _userRepository.GetAllAsync())
        {
            await _userRepository.HardDeleteAsync(entity);
        }

        foreach (var user in backup.Users)
        {
            await _userRepository.AddAsync(new UserEntity(user.UserId, user.heightInCm));
        }
        foreach (var weightRecord in backup.WeightRecords)
        {
            await _weightRepository.AddAsync(new WeightEntity(
                weightRecord.WeightId == Guid.Empty ? Guid.NewGuid() : weightRecord.WeightId,
                weightRecord.UserId,
                weightRecord.Date,
                weightRecord.Weight,
                weightRecord.Deleted,
                weightRecord.Comment));
        }

        return Ok();
    }
}
