using System.Text.Json.Nodes;
using Api.Controllers.Models;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Controllers;

[ApiController]
[Route("api/weight")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class WeightController : ControllerBase
{
    private readonly DataAccess.WeightRepository _weightRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WeightController(DataAccess.WeightRepository weightRepository, IHttpContextAccessor httpContextAccessor)
    {
        _weightRepository = weightRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet("/api/weights")]
    public async Task<WeightsCollection> GetAll()
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entities = await _weightRepository.GetAllAsync(userId);
        var weightRecords = entities.Select(entity => new WeightRecord(
            entity.WeightId,
            entity.UserId,
            entity.Date,
            entity.Weight,
            entity.Deleted,
            entity.Comment));
        var orderedWeightRecords = weightRecords.OrderBy(record => record.Date);
        return new WeightsCollection(orderedWeightRecords);
    }

    [HttpGet("{weightId:guid}")]
    public async Task<IActionResult> GetWeightRecord(Guid weightId)
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entity = await _weightRepository.GetByIdAsync(weightId, userId);
        if (entity == null)
        {
            return NotFound($"Weight record with ID {weightId} not found.");
        }
        return Ok(new WeightRecord(
            entity.WeightId,
            entity.UserId,
            entity.Date,
            entity.Weight,
            entity.Deleted,
            entity.Comment));
    }

    [HttpPost]
    public async Task<IActionResult> PostWeightRecord([FromBody] CreateWeightRecordRequest request)
    {
        var weightId = Guid.NewGuid();
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entity = new WeightEntity(weightId, userId, request.Date, request.Weight, false, request.Comment);
        await _weightRepository.AddAsync(entity);
        Response.Headers.Append("Location", $"/api/weight/{weightId}");
        return CreatedAtAction(nameof(GetWeightRecord), new { weightId }, null);
    }

    [HttpPut("{weightId:guid}")]
    public async Task<IActionResult> UpdateWeightRecord(Guid weightId, [FromBody] UpdateWeightRecordRequest request)
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entity = await _weightRepository.GetByIdAsync(weightId, userId);
        if (entity == null)
        {
            return NotFound($"Weight record with ID {weightId} not found.");
        }

        entity.Comment = request.Comment;
        await _weightRepository.UpdateAsync(entity);
        return NoContent();
    }

    [HttpDelete("{weightId:guid}")]
    public async Task<IActionResult> DeleteWeightRecord(Guid weightId)
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entityToDelete = await _weightRepository.GetByIdAsync(weightId, userId);
        if (entityToDelete != null)
        {
            await _weightRepository.DeleteAsync(entityToDelete);
        }

        return NoContent();
    }

    public class WeightRecordExample : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(WeightRecord) && schema is OpenApiSchema concreteSchema)
                concreteSchema.Example = new JsonObject
                {
                    ["date"] = "2025-01-01T00:00:00Z",
                    ["weight"] = 75.5,
                    ["userId"] = "ApiUser",
                    ["comment"] = "after a big lunch",
                };
        }
    }
}