using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace userinterface.ViewModels
{
    public class ProfilesViewModel : ViewModelBase
    {
        public ProfilesViewModel()
        {
        }

        public string ProfileOneTitle { get; set; } = "Profile1";

        public ISeries[] Series { get; } =
        {
            new LineSeries<double>
            {
                Values = new double[] { 5, 0, 5, 0, 5, 0 },
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0,
            },
            new LineSeries<double>
            {
                Values = new double[] { 7, 2, 7, 2, 7, 2 },
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 1,
            },
        };

        public string ChartTitle { get; } = "Test Chart";
    }
}
