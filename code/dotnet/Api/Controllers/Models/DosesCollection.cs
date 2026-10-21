namespace Api.Controllers.Models;

public record DosesCollection(IEnumerable<DoseRecord>? DoseRecords);
