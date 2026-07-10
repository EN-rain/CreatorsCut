using CreatorCut.Domain;

namespace CreatorCut.Application;

public sealed class SplitClipCommand
{
    public ProjectId ProjectId { get; init; }
}
