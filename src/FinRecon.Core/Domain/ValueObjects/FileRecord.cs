using FinRecon.Core.Domain.Enums;

namespace FinRecon.Core.Domain.ValueObjects;

public record FileRecord(string ClientId, ProductType ProductType, decimal Value);
