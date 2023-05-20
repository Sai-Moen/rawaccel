using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;

using System.Collections.Generic;

namespace RawAccel.ViewModels
{
    public sealed class ChartViewModel : ViewModelBase
    {
        public const int MaxPoints = 0x1000;

        public ChartViewModel()
        {
            LastMouseMoveList = new Queue<ObservablePoint>(MaxPoints);

            LastMouseMoveSeries =
            new LineSeries<ObservablePoint>
            {
                Values = LastMouseMoveList,
                AnimationsSpeed = System.TimeSpan.FromMilliseconds(10),
                GeometrySize = 0,
            };

            BaseSeries =
            new LineSeries<ObservablePoint>
            {
                Values = new ObservablePoint[]
                {
                    new ObservablePoint(0, 0),
                }
            };

            Series = new ISeries[]
            {
                BaseSeries,
                LastMouseMoveSeries,
            };
        }

        private readonly Queue<ObservablePoint> LastMouseMoveList;

        public LineSeries<ObservablePoint> LastMouseMoveSeries;

        public LineSeries<ObservablePoint> BaseSeries;

        public ISeries[] Series { get; }

        public void SetLastMouseMove(float x, float y)
        {
            if (LastMouseMoveList.Count >= MaxPoints)
            {
                _ = LastMouseMoveList.Dequeue();
            }
            LastMouseMoveList.Enqueue(new ObservablePoint(x, y));
        }
    }
}
