using System.Collections.Generic;
using userinterface.Models.Mouse;

namespace userinterface.ViewModels;

public sealed class ChartsViewModel : ViewModelBase, IMouseMoveDisplayer
{
    private List<ChartViewModel> charts = new();

    public void SetLastMouseMove(float x, float y)
    {
        foreach (ChartViewModel chart in charts)
        {
            chart.SetLastMouseMove(x, y);
        }
    }
}
