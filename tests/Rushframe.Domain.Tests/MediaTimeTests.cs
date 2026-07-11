namespace Rushframe.Domain.Tests;

public sealed class MediaTimeTests
{
    [Fact]
    public void FromSeconds_round_trips()
    {
        var t = MediaTime.FromSeconds(15.5);
        Assert.Equal(15.5, t.Seconds, 3);
    }

    [Fact]
    public void Add_preserves_duration()
    {
        var a = MediaTime.FromSeconds(10);
        var b = MediaTime.FromSeconds(5);
        Assert.Equal(15, (a + b).Seconds, 3);
    }

    [Fact]
    public void Subtract_works()
    {
        var a = MediaTime.FromSeconds(10);
        var b = MediaTime.FromSeconds(3);
        Assert.Equal(7, (a - b).Seconds, 3);
    }

    [Fact]
    public void Zero_is_zero()
    {
        Assert.Equal(0, MediaTime.Zero.Seconds);
    }
}
