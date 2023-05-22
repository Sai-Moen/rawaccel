using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace RawAccel.ViewModels
{
    public sealed class ChartViewModel : ViewModelBase
    {
        public ChartViewModel()
        {
            LastMouseMove = new ObservablePoint(0, 0);

            LastMouseMoveSeries = new LineSeries<ObservablePoint>
            {
                Values = new ObservablePoint[]
                {
                    LastMouseMove,
                },
                AnimationsSpeed = System.TimeSpan.FromMilliseconds(10),
                EnableNullSplitting = false,
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

        private readonly ObservablePoint LastMouseMove;

        private readonly LineSeries<ObservablePoint> LastMouseMoveSeries;

        public ISeries<ObservablePoint>[] Series { get; }

        public void SetLastMouseMove(float x, float y)
        {
            LastMouseMove.X = x;
            LastMouseMove.Y = y;
        }
    }
}
