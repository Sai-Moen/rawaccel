using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

using System.Collections.ObjectModel;

namespace RawAccel.ViewModels
{
    public sealed class ChartViewModel : ViewModelBase
    {
        public ChartViewModel()
        {
            LastMouseMoveList = new ObservableCollection<ObservablePoint>
            {
                new ObservablePoint(0, 0),
            };

            LastMouseMoveSeries = new LineSeries<ObservablePoint>
            {
                Values = LastMouseMoveList, // How to remove that weird shit
            };

            Series = new ISeries<ObservablePoint>[]
            {
                BaseSeries,
                LastMouseMoveSeries,
            };
        }

        private const int size = 64; // temporary variable, Permanent Laziness

        private readonly LineSeries<ObservablePoint> BaseSeries = new()
        {
            Values = new ObservablePoint[]
            {
                new ObservablePoint(0, 0),
                new ObservablePoint(size, size),
            },
            GeometrySize = 0,
            LineSmoothness = 1,
        };

        private readonly ObservableCollection<ObservablePoint> LastMouseMoveList;

        private readonly LineSeries<ObservablePoint> LastMouseMoveSeries;

        public ISeries<ObservablePoint>[] Series { get; }

        public void SetLastMouseMove(float x, float y)
        {
            LastMouseMoveList.Clear();
            LastMouseMoveList.Add(new ObservablePoint(x, y));
        }
    }
}
