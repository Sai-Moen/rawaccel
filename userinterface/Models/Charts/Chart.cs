namespace userinterface.Models.Charts;

public class Chart
{
    public string? Title { get; set; }

    public double Width { get; set; }
    public double Height { get; set; }

    public Graph? Graph { get; set; }

    public Axis? AxisX { get; set; }
    public Axis? AxisY { get; set; }

    public bool IsReady => Graph is not null && AxisX is not null && AxisY is not null;
}
