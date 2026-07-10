namespace CreatorCut.Domain;

public readonly record struct MediaTime
{
    private readonly long _numerator;
    private readonly long _denominator;

    public long Numerator => _numerator;
    public long Denominator => _denominator > 0 ? _denominator : 1;

    public MediaTime(long numerator, long denominator)
    {
        _numerator = numerator;
        _denominator = denominator > 0 ? denominator : 1;
    }

    public static readonly MediaTime Zero = new(0, 1);

    public double Seconds => (double)Numerator / Denominator;

    public static MediaTime FromSeconds(double seconds)
    {
        var den = 1_000_000;
        return new MediaTime((long)(seconds * den), den);
    }

    public static MediaTime FromFrame(int frame, double fps)
    {
        var den = (int)Math.Round(fps * 1_000);
        return new MediaTime(frame * den, den);
    }

    public MediaTime Add(MediaTime other)
    {
        var common = Denominator * other.Denominator;
        return new MediaTime(
            Numerator * other.Denominator + other.Numerator * Denominator,
            common
        );
    }

    public MediaTime Subtract(MediaTime other)
    {
        var common = Denominator * other.Denominator;
        return new MediaTime(
            Numerator * other.Denominator - other.Numerator * Denominator,
            common
        );
    }

    public static MediaTime operator +(MediaTime a, MediaTime b) => a.Add(b);
    public static MediaTime operator -(MediaTime a, MediaTime b) => a.Subtract(b);
    public static bool operator <(MediaTime a, MediaTime b) => a.Seconds < b.Seconds;
    public static bool operator >(MediaTime a, MediaTime b) => a.Seconds > b.Seconds;
    public static bool operator <=(MediaTime a, MediaTime b) => a.Seconds <= b.Seconds;
    public static bool operator >=(MediaTime a, MediaTime b) => a.Seconds >= b.Seconds;
}
