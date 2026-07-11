namespace Rushframe.Domain;

public sealed class StabilizationSettings
{
    public bool Enabled { get; set; }
    public double Strength { get; set; } = 0.5;
    public bool CropZoomCompensation { get; set; } = true;
    public bool AnalysisComplete { get; set; }
}
