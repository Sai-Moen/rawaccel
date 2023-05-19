using Avalonia.Threading;

using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;

using userinterface.Models.Settings;

namespace userinterface.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        public const int MaxPoints = 0x1000;

        public ProfilesViewModel()
        {
            LastMouseMoveList = new List<ObservablePoint>(MaxPoints);

            LastMouseMoveSeries =
            new LineSeries<ObservablePoint>
            {
                Values = LastMouseMoveList,
                AnimationsSpeed = System.TimeSpan.FromMilliseconds(10),
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

            Profiles = new HashSet<Profile>();
        }

        public IList<ObservablePoint> LastMouseMoveList;

        public LineSeries<ObservablePoint> LastMouseMoveSeries;

        public LineSeries<ObservablePoint> BaseSeries;

        public ISeries[] Series { get; }

        public ISet<Profile> Profiles { get; set; }

        public void SetLastMouseMove(float x, float y)
        {
            if (LastMouseMoveList.Count >= MaxPoints)
            {
                LastMouseMoveList.RemoveAt(0);
            }
            LastMouseMoveList.Add(new ObservablePoint(x, y));
        }
    }
}
