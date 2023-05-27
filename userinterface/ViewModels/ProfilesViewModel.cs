using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottable;

namespace userinterface.ViewModels
{
    public sealed class ProfilesViewModel : ViewModelBase
    {
        #region Constants

        public const string SensitivityXName = "SensitivityX";
        public const string GainXName = "GainX";
        public const string VelocityXName = "VelocityX";

        public const string SensitivityYName = "SensitivityY";
        public const string GainYName = "GainY";
        public const string VelocityYName = "VelocityY";

        #endregion Constants

        #region Fields

        private int LMMCount;
        private double AccumulatorX;
        private double AccumulatorY;

        private readonly double[] LastMouseMoveX = new double[1];
        private readonly double[] LastMouseMoveY = new double[1];
        private readonly SignalPlotXYGeneric<double, double> LastMouseMove;

        #endregion Fields

        #region Constructors

        public ProfilesViewModel()
        {
            LastMouseMove = new()
            {
                Xs = LastMouseMoveX,
                Ys = LastMouseMoveY,
            };
        }

        #endregion Constructors

        #region Properties

        private AvaPlot? _sX;
        public AvaPlot SensitivityX
        {
            get => _sX!;
            set
            {
                _sX = value;
                _sX.Plot.Add(LastMouseMove);
                _sX.Plot.AddSignalConst(new double[] { 0, 64 }, sampleRate: 0.015625);
            }
        }

        //public AvaPlot GainX        { get; set; }
        //public AvaPlot VelocityX    { get; set; }

        //public AvaPlot SensitivityY { get; set; }
        //public AvaPlot GainY        { get; set; }
        //public AvaPlot VelocityY    { get; set; }

        #endregion Properties

        #region Methods

        public void SetLastMouseMove(float x, float y)
        {
            const int samples = 12;

            AccumulatorX += x;
            AccumulatorY += y;

            LMMCount = ++LMMCount % samples;
            if (LMMCount != 0) return;

            LastMouseMoveX[0] = AccumulatorX / samples;
            LastMouseMoveY[0] = AccumulatorY / samples;

            const RenderType renderType = RenderType.HighQualityDelayed;

            SensitivityX.RefreshRequest(renderType);
            //GainX.RefreshRequest();
            //VelocityX.RefreshRequest();

            //SensitivityY.RefreshRequest();
            //GainY.RefreshRequest();
            //VelocityY.RefreshRequest();

            AccumulatorX = 0;
            AccumulatorY = 0;
        }

        #endregion Methods
    }
}
