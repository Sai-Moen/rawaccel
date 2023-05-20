using LiveChartsCore.Defaults;

namespace RawAccel.Models.Mouse
{
    public interface IMouseMoveDisplayer
    {
        public void SetLastMouseMove(float x, float y);
    }
}
