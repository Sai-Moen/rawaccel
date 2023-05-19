using System.Windows.Forms;

namespace userinterface.Models.Mouse
{
    public class MouseWindow : WinApiServiceBase
    {
        public MouseWindow(IMouseMoveDisplayer displayer)
        {
            MouseWatcher = new MouseWatcher(displayer, SpongeHandle);
        }

        protected MouseWatcher MouseWatcher { get; set; }

        protected override void WndProc(Message message)
        {
            if (message.Msg == 0x00ff) // WM_INPUT
            {
                MouseWatcher.ReadMouseMove(message);
            }
        }
    }
}
