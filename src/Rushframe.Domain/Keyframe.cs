namespace Rushframe.Domain;

public enum InterpolationType { Hold, Linear, EaseIn, EaseOut, EaseInOut, Bezier }

public sealed class Keyframe
{
    public KeyframeId Id { get; init; } = KeyframeId.New();
    public required MediaTime Time { get; init; }
    public required double Value { get; set; }
    public InterpolationType Interpolation { get; set; } = InterpolationType.Linear;
    public double InTangentX { get; set; } = 0.25;
    public double InTangentY { get; set; } = 0.25;
    public double OutTangentX { get; set; } = 0.75;
    public double OutTangentY { get; set; } = 0.75;
}

public sealed class AnimatedProperty
{
    public required string PropertyName { get; init; }
    public double DefaultValue { get; set; }
    public List<Keyframe> Keyframes { get; init; } = [];

    public double GetValueAt(MediaTime time)
    {
        if (Keyframes.Count == 0) return DefaultValue;

        var sorted = Keyframes.OrderBy(k => k.Time.Seconds).ToList();
        if (time <= sorted[0].Time) return sorted[0].Value;
        if (time >= sorted[^1].Time) return sorted[^1].Value;

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (time >= sorted[i].Time && time <= sorted[i + 1].Time)
            {
                var t0 = sorted[i].Time.Seconds;
                var t1 = sorted[i + 1].Time.Seconds;
                var v0 = sorted[i].Value;
                var v1 = sorted[i + 1].Value;
                var frac = (time.Seconds - t0) / (t1 - t0);

                return sorted[i].Interpolation switch
                {
                    InterpolationType.Hold => v0,
                    InterpolationType.Linear => v0 + (v1 - v0) * frac,
                    InterpolationType.EaseIn => v0 + (v1 - v0) * (frac * frac),
                    InterpolationType.EaseOut => v0 + (v1 - v0) * (1 - (1 - frac) * (1 - frac)),
                    InterpolationType.EaseInOut => v0 + (v1 - v0) * (frac * frac * (3 - 2 * frac)),
                    _ => v0 + (v1 - v0) * frac,
                };
            }
        }

        return DefaultValue;
    }
}
