using Rushframe.Domain;

namespace Rushframe.Infrastructure;

public sealed class EffectRegistry
{
    private readonly Dictionary<string, EffectDefinition> _definitions = [];

    public EffectRegistry()
    {
        RegisterBuiltIn();
    }

    private void RegisterBuiltIn()
    {
        Register(new EffectDefinition
        {
            EffectTypeId = "mono", Name = "Monochrome", Category = "color",
            Parameters = [new EffectParameter { Name = "intensity", Type = "float", DefaultValue = 1.0, Min = 0, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "sepia", Name = "Sepia", Category = "retro",
            Parameters = [new EffectParameter { Name = "intensity", Type = "float", DefaultValue = 1.0, Min = 0, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "blur", Name = "Blur", Category = "distortion",
            Parameters = [new EffectParameter { Name = "strength", Type = "float", DefaultValue = 5.0, Min = 0, Max = 50 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "brightness", Name = "Brightness", Category = "color",
            Parameters = [new EffectParameter { Name = "amount", Type = "float", DefaultValue = 0.1, Min = -1, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "contrast", Name = "Contrast", Category = "color",
            Parameters = [new EffectParameter { Name = "amount", Type = "float", DefaultValue = 0.1, Min = -1, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "vignette", Name = "Vignette", Category = "style",
            Parameters = [
                new EffectParameter { Name = "strength", Type = "float", DefaultValue = 0.5, Min = 0, Max = 1 },
                new EffectParameter { Name = "feather", Type = "float", DefaultValue = 0.5, Min = 0, Max = 1 }
            ]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "noise_reduction", Name = "Noise Reduction", Category = "quality",
            Parameters = [new EffectParameter { Name = "strength", Type = "float", DefaultValue = 0.3, Min = 0, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "motion_blur", Name = "Motion Blur", Category = "lens",
            Parameters = [
                new EffectParameter { Name = "strength", Type = "float", DefaultValue = 0.5, Min = 0, Max = 1 },
                new EffectParameter { Name = "samples", Type = "int", DefaultValue = 8, Min = 2, Max = 64 }
            ]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "glitch", Name = "Glitch", Category = "distortion",
            Parameters = [new EffectParameter { Name = "intensity", Type = "float", DefaultValue = 0.3, Min = 0, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "film_grain", Name = "Film Grain", Category = "retro",
            Parameters = [new EffectParameter { Name = "intensity", Type = "float", DefaultValue = 0.2, Min = 0, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "food_pop", Name = "Food Pop", Category = "food",
            Parameters =
            [
                new EffectParameter { Name = "saturation", Type = "float", DefaultValue = 1.25, Min = 0, Max = 3 },
                new EffectParameter { Name = "warmth", Type = "float", DefaultValue = 0.08, Min = -1, Max = 1 },
            ]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "night_lift", Name = "Night Lift", Category = "night",
            Parameters =
            [
                new EffectParameter { Name = "brightness", Type = "float", DefaultValue = 0.08, Min = -1, Max = 1 },
                new EffectParameter { Name = "contrast", Type = "float", DefaultValue = 0.15, Min = -1, Max = 1 },
            ]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "nature_green", Name = "Nature Green", Category = "nature",
            Parameters = [new EffectParameter { Name = "intensity", Type = "float", DefaultValue = 0.35, Min = 0, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "spark_flash", Name = "Spark Flash", Category = "spark",
            Parameters = [new EffectParameter { Name = "intensity", Type = "float", DefaultValue = 0.5, Min = 0, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "love_soft", Name = "Love Soft", Category = "love",
            Parameters = [new EffectParameter { Name = "softness", Type = "float", DefaultValue = 0.25, Min = 0, Max = 1 }]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "lens_punch", Name = "Lens Punch", Category = "lens",
            Parameters =
            [
                new EffectParameter { Name = "zoom", Type = "float", DefaultValue = 1.08, Min = 1, Max = 2 },
                new EffectParameter { Name = "vignette", Type = "float", DefaultValue = 0.25, Min = 0, Max = 1 },
            ]
        });
        Register(new EffectDefinition
        {
            EffectTypeId = "body_smooth_motion", Name = "Smooth Motion", Category = "body/motion",
            Parameters = [new EffectParameter { Name = "strength", Type = "float", DefaultValue = 0.35, Min = 0, Max = 1 }]
        });
    }

    public void Register(EffectDefinition def)
    {
        _definitions[def.EffectTypeId] = def;
    }

    public EffectDefinition? Get(string effectTypeId) =>
        _definitions.GetValueOrDefault(effectTypeId);

    public List<EffectDefinition> GetAll() => [.._definitions.Values];

    public List<EffectDefinition> GetByCategory(string category) =>
        _definitions.Values.Where(d => d.Category == category).ToList();
}
