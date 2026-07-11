namespace Rushframe.Domain;

public readonly record struct SequenceId(Guid Value)
{
    public static SequenceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct TrackId(Guid Value)
{
    public static TrackId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct TimelineItemId(Guid Value)
{
    public static TimelineItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct MediaAssetId(Guid Value)
{
    public static MediaAssetId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct EffectInstanceId(Guid Value)
{
    public static EffectInstanceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct KeyframeId(Guid Value)
{
    public static KeyframeId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct MarkerId(Guid Value)
{
    public static MarkerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}
