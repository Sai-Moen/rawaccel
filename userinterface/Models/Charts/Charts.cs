namespace userinterface.Models.Charts;

public class Charts
{
    public Chart Sensitivity { get; }
    public Chart Gain { get; }
    public Chart Velocity { get; }

    public Charts(Chart s, Chart g, Chart v)
    {
        Sensitivity = s;
        Gain = g;
        Velocity = v;
    }
}
