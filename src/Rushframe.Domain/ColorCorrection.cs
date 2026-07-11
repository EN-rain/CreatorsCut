namespace Rushframe.Domain;

public sealed class ColorCorrection
{
    public double Brightness { get; set; }
    public double Contrast { get; set; }
    public double Saturation { get; set; } = 1.0;
    public double Exposure { get; set; }
    public double Highlights { get; set; }
    public double Shadows { get; set; }
    public double Whites { get; set; }
    public double Blacks { get; set; }
    public double Tint { get; set; }
    public bool BlackAndWhite { get; set; }
}
