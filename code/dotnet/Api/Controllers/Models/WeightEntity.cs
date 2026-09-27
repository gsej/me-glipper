using System.Globalization;

namespace Api.Controllers.Models;

public class WeightEntity
{
    public WeightEntity()
    {
    }

    public WeightEntity(Guid weightId, string userId, DateTime date, decimal weight, bool deleted, string? comment = null)
    {
        WeightId = weightId;
        UserId = userId;
        Date = date;
        Weight = weight;
        Deleted = deleted;
        Comment = comment;
    }

    public Guid WeightId { get; init; }

    public string UserId { get; init; } = null!;

    public DateTime Date { get; init; }

    public bool Deleted { get; set; } = false;

    public decimal Weight { get; set; }

    public string? Comment { get; set; }
}
