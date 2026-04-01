namespace FinRecon.Infrastructure.Messaging.Messages;

/// <summary>
/// Message published by the API when a reconciliation job is successfully created.
/// Consumed by FinRecon.Worker to trigger async processing.
/// </summary>
public record ReconciliationJobCreated(Guid JobId);
