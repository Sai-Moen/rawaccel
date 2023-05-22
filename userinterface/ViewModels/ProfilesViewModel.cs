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
            LastMouseMoveList = new ObservableCollection<ObservablePoint>();

            LastMouseMoveSeries =
            new ScatterSeries<ObservablePoint>
            {
                Values = LastMouseMoveList,
                AnimationsSpeed = System.TimeSpan.FromMilliseconds(1),
            };

            Series = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Values = new ObservablePoint[]
                    {
                        new ObservablePoint(0, 1),
                        new ObservablePoint(100, 1),
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

        private ObservableCollection<ObservablePoint> LastMouseMoveList { get; }

        public string ChartTitle { get; } = "Test Chart";

        public void SetLastMouseMove(float x, float y)
        {
            if (LastMouseMoveList.Count == 0)
            {
                LastMouseMoveList.Add(new ObservablePoint(x, y));
            }
            else
            {
                LastMouseMoveList[0].X = x;
                LastMouseMoveList[0].Y = y;
            }
        }
    }
}
