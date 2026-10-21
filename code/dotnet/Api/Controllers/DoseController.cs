using System.Text.Json.Nodes;
using Api.Controllers.Models;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Controllers;

[ApiController]
[Route("api/dose")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class DoseController : ControllerBase
{
    private readonly DataAccess.DoseRepository _doseRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DoseController(DataAccess.DoseRepository doseRepository, IHttpContextAccessor httpContextAccessor)
    {
        _doseRepository = doseRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet("/api/doses")]
    public async Task<DosesCollection> GetAll()
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entities = await _doseRepository.GetAllAsync(userId);
        var doseRecords = entities.Select(entity => new DoseRecord(
            entity.DoseId,
            entity.UserId,
            entity.TakenAt,
            entity.MedicationName,
            entity.DoseAmountMg,
            entity.Deleted,
            entity.Notes));
        var orderedDoseRecords = doseRecords.OrderBy(record => record.TakenAt);
        return new DosesCollection(orderedDoseRecords);
    }

    [HttpGet("{doseId:guid}")]
    public async Task<IActionResult> GetDoseRecord(Guid doseId)
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entity = await _doseRepository.GetByIdAsync(doseId, userId);
        if (entity == null)
        {
            return NotFound($"Dose record with ID {doseId} not found.");
        }
        return Ok(new DoseRecord(
            entity.DoseId,
            entity.UserId,
            entity.TakenAt,
            entity.MedicationName,
            entity.DoseAmountMg,
            entity.Deleted,
            entity.Notes));
    }

    [HttpPost]
    public async Task<IActionResult> PostDoseRecord([FromBody] CreateDoseRequest request)
    {
        var doseId = Guid.NewGuid();
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entity = new DoseEntity(doseId, userId, request.TakenAt, request.MedicationName, request.DoseAmountMg, false, request.Notes);
        await _doseRepository.AddAsync(entity);
        Response.Headers.Append("Location", $"/api/dose/{doseId}");
        return CreatedAtAction(nameof(GetDoseRecord), new { doseId }, null);
    }

    [HttpPut("{doseId:guid}")]
    public async Task<IActionResult> UpdateDoseRecord(Guid doseId, [FromBody] UpdateDoseRequest request)
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entity = await _doseRepository.GetByIdAsync(doseId, userId);
        if (entity == null)
        {
            return NotFound($"Dose record with ID {doseId} not found.");
        }

        entity.Notes = request.Notes;
        await _doseRepository.UpdateAsync(entity);
        return NoContent();
    }

    [HttpDelete("{doseId:guid}")]
    public async Task<IActionResult> DeleteDoseRecord(Guid doseId)
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entityToDelete = await _doseRepository.GetByIdAsync(doseId, userId);
        if (entityToDelete != null)
        {
            await _doseRepository.DeleteAsync(entityToDelete);
        }

        return NoContent();
    }

    public class DoseRecordExample : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(DoseRecord) && schema is OpenApiSchema concreteSchema)
                concreteSchema.Example = new JsonObject
                {
                    ["takenAt"] = "2025-01-01T08:30:00Z",
                    ["medicationName"] = "Semaglutide",
                    ["doseAmountMg"] = 1.0,
                    ["userId"] = "ApiUser",
                    ["notes"] = "first dose of the week",
                };
        }
    }
}
