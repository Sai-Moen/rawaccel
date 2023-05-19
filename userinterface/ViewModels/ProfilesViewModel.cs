using Avalonia.Threading;

using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Documents;

using userinterface.Models.Settings;

namespace userinterface.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        public ProfilesViewModel()
        {
            Profiles = new List<Profile>();

            LastMouseMoveList = new ObservableCollection<WeightedPoint>();

            LastMouseMoveSeries =
            new ScatterSeries<ObservablePoint>
            {
                Values = new ObservableCollection<ObservablePoint>(),
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

        public IList<Profile> Profiles { get; set; }

        public ISeries[] Series { get; }

        private ScatterSeries<ObservablePoint> LastMouseMoveSeries { get; set; }

        private ObservableCollection<WeightedPoint> LastMouseMoveList { get; }

        public void SetLastMouseMove(float x, float y)
        {
            LastMouseMoveList.Add(new WeightedPoint(x, y, 1));
        }
    }
}
