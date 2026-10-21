using System.ComponentModel.DataAnnotations;

namespace Api.Controllers.Models;

public record UpdateDoseRequest([MaxLength(200)] string? Notes = null);
