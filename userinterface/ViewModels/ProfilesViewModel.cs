using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Documents;

namespace userinterface.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        public ProfilesViewModel()
        {
            LastMouseMoveList = new ObservableCollection<WeightedPoint>();

            LastMouseMoveSeries =
            new ScatterSeries<ObservablePoint>
            {
                Values = LastMouseMoveList,
                AnimationsSpeed = System.TimeSpan.FromMilliseconds(10),
                //EasingFunction = t => t * t,
            };

            Series = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Values = new ObservablePoint[]
                    {
                        new ObservablePoint(0, 0),
                        new ObservablePoint(1, 1),
                        new ObservablePoint(10, 10),
                        new ObservablePoint(50, 50),
                    },
                    Fill = null,
                    GeometrySize = 0,
                    LineSmoothness = 0,
                },

                LastMouseMoveSeries,
            };
        }

        public string ProfileOneTitle { get; set; } = "Profile1";

        public ISeries[] Series { get; }

        private ScatterSeries<ObservablePoint> LastMouseMoveSeries { get; set; }

        private ObservableCollection<WeightedPoint> LastMouseMoveList { get; }

        public string ChartTitle { get; } = "Test Chart";

        public void SetLastMouseMove(float x, float y)
        {
            LastMouseMoveList.Add(new WeightedPoint(x, y, 1));
        }
    }
}
