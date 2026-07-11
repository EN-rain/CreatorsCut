namespace Rushframe.Domain;

public enum MaskShape { Rectangle, Ellipse, Linear, Mirror, Star, Polygon, Split }

public sealed class Mask
{
    public MaskShape Shape { get; init; } = MaskShape.Rectangle;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
    public double RotationDegrees { get; set; }
    public double Feather { get; set; }
    public double Expansion { get; set; }
    public bool Inverted { get; set; }
}
