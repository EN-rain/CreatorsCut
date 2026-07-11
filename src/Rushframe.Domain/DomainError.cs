namespace Rushframe.Domain;

public abstract record DomainError(string Message);

public sealed record ItemNotFoundError(TimelineItemId ItemId) : DomainError($"Timeline item {ItemId} not found");
public sealed record TrackNotFoundError(TrackId TrackId) : DomainError($"Track {TrackId} not found");
public sealed record SequenceNotFoundError(SequenceId SequenceId) : DomainError($"Sequence {SequenceId} not found");
public sealed record InvalidOperationError(string Message) : DomainError(Message);
public sealed record ValidationError(string Field, string Message) : DomainError($"Validation failed on '{Field}': {Message}");
