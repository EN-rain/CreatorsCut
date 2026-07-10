namespace CreatorCut.Domain;

public enum ItemKind { Clip, Text, Image, Sticker, AdjustmentLayer }

public sealed class TimelineItem
{
    public TimelineItemId Id { get; init; } = TimelineItemId.New();
    public ItemKind Kind { get; init; }
    public MediaAssetId? MediaAssetId { get; init; }
    public MediaTime TimelineStart { get; set; }
    public MediaTime Duration { get; set; }
    public MediaTime SourceStart { get; set; }
    public MediaTime SourceDuration { get; set; }
    public double Speed { get; set; } = 1.0;
    public bool Reversed { get; set; }
    public double Volume { get; set; } = 1.0;
    public bool Muted { get; set; }
    public Transform2D Transform { get; init; } = new();
    public double Opacity { get; set; } = 1.0;
    public bool Locked { get; set; }

    public string? TextContent { get; set; }
    public string? FontFamily { get; set; }
    public double FontSize { get; set; } = 48;
    public bool FontBold { get; set; }
    public string FontAlign { get; set; } = "center";
    public string? FillColor { get; set; }
    public string? OutlineColor { get; set; }
    public double OutlineWidth { get; set; }
    public string? ShadowColor { get; set; }
    public double ShadowOffsetX { get; set; } = 2;
    public double ShadowOffsetY { get; set; } = 2;
    public double ShadowBlur { get; set; } = 4;
    public double ShadowOpacity { get; set; } = 0.5;

    public MediaTime FadeInDuration { get; set; }
    public MediaTime FadeOutDuration { get; set; }
    public double Pan { get; set; }
    public double CropLeft { get; set; }
    public double CropTop { get; set; }
    public double CropRight { get; set; }
    public double CropBottom { get; set; }

    public BlendMode BlendMode { get; set; }
    public List<EffectInstance> Effects { get; init; } = [];
    public AnimatedProperty? AnimatedProperty { get; set; }
    public ChromaKey? ChromaKey { get; set; }

    public MediaTime SourceEnd => MediaTime.FromSeconds(SourceStart.Seconds + (SourceDuration.Seconds / Speed));
    public MediaTime TimelineEnd => TimelineStart.Add(Duration);
}

public sealed class Transform2D
{
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
    public double RotationDegrees { get; set; }
    public double AnchorX { get; set; }
    public double AnchorY { get; set; }
}

public sealed class ChromaKey
{
    public string? Color { get; set; }
    public double Similarity { get; set; } = 0.1;
    public double Intensity { get; set; } = 0.1;
    public double EdgeSoftness { get; set; } = 0.05;
    public double SpillSuppression { get; set; } = 0.1;
    public bool ShadowSuppression { get; set; }
}
