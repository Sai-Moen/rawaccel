using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using userspace_backend.Display;

namespace userinterface.ViewModels
{
    public partial class ProfileChartViewModel : ViewModelBase
    {
        public ProfileChartViewModel(ICurvePreview curvePreview)
        {
            Series =
            [
                new LineSeries<CurvePoint>
                {
                    Values = curvePreview.Points,
                    Fill = null,
                    Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                }
            ];
            YAxes =
            [
                new Axis()
                {
                    MinZoomDelta = 1,
                    MinLimit = 0,
                }
            ];
        }

        public ISeries[] Series { get; set; }

        public Axis[] XAxes { get; set; }

        public Axis[] YAxes { get; set; }
    }
}
