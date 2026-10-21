using System.ComponentModel.DataAnnotations;

namespace Api.Controllers.Models;

public record CreateDoseRequest(
    DateTime TakenAt,
    [Required] string MedicationName,
    decimal DoseAmountMg,
    [MaxLength(200)] string? Notes = null);
