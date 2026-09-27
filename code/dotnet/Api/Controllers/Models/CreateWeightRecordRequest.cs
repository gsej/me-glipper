using System.ComponentModel.DataAnnotations;

namespace Api.Controllers.Models;

public record CreateWeightRecordRequest(DateTime Date, decimal Weight, [MaxLength(200)] string? Comment = null);
