using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace userinterface.ViewModels
{
    public partial class ProfileChartViewModel : ViewModelBase
    {
        public ProfileChartViewModel()
        {
        }

        public ISeries[] Series { get; set; } 
            = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new double[] { 2, 1, 3, 5, 3, 4, 6 },
                    Fill = null
                }
            };

    }
}
