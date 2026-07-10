namespace CreatorCut.Domain;

public sealed class EffectDefinition
{
    public required string EffectTypeId { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public List<EffectParameter> Parameters { get; init; } = [];
}

public sealed class EffectParameter
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required object DefaultValue { get; init; }
    public double Min { get; init; }
    public double Max { get; init; } = 1.0;
}

public sealed class EffectInstance
{
    public EffectInstanceId Id { get; init; } = EffectInstanceId.New();
    public required string EffectTypeId { get; init; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object> Parameters { get; init; } = [];
}
