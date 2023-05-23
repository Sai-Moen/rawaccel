using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace RawAccel.ViewModels
{
    public sealed class ChartViewModel : ViewModelBase
    {
        public ChartViewModel()
        {
            LastMouseMoveSeries = new LineSeries<ObservablePoint>
            {
                Values = new ObservablePoint[]
                {
                    LastMouseMove,
                },
                AnimationsSpeed = System.TimeSpan.Zero,
            };

            Series = new ISeries<ObservablePoint>[]
            {
                BaseSeries,
                LastMouseMoveSeries,
            };
        }

        private const int size = 128; // temporary variable, Permanent Laziness

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

        private readonly ObservablePoint LastMouseMove = new(0.0, 0.0);

        private readonly LineSeries<ObservablePoint> LastMouseMoveSeries;

        public ISeries<ObservablePoint>[] Series { get; }

        public void SetLastMouseMove(float x, float y)
        {
            LastMouseMove.X = x;
            LastMouseMove.Y = y;
        }
    }
}
