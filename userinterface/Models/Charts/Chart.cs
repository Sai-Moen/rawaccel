namespace userinterface.Models.Charts;

public class Chart
{
    public string? Title { get; init; }

    public double Width { get; init; }
    public double Height { get; init; }

    public Graph? Graph { get; init; }

    public Axis? AxisX { get; init; }
    public Axis? AxisY { get; init; }

    public bool IsReady => Graph is not null && AxisX is not null && AxisY is not null;
}
