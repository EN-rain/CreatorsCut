namespace CreatorCut.Domain.Tests;

public sealed class ProjectTests
{
    [Fact]
    public void new_project_has_default_sequence()
    {
        var p = new Project();
        Assert.NotNull(p.MainSequence);
        Assert.Single(p.Sequences);
    }

    [Fact]
    public void add_sequence_increases_count()
    {
        var p = new Project();
        p.AddSequence("Extra");
        Assert.Equal(2, p.Sequences.Count);
    }

    [Fact]
    public void project_id_is_unique()
    {
        var a = ProjectId.New();
        var b = ProjectId.New();
        Assert.NotEqual(a, b);
    }
}
