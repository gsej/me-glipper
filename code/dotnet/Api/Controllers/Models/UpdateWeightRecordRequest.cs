using System.ComponentModel.DataAnnotations;

namespace Api.Controllers.Models;

public record UpdateWeightRecordRequest([MaxLength(200)] string? Comment = null);
