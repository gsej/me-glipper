namespace Api.Controllers.Models;

public record WeightRecord(
    Guid WeightId,
    string UserId,
    DateTime Date,
    decimal Weight,
    bool Deleted,
    string? Comment = null);