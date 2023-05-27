using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ScottPlot.Avalonia;
using userinterface.ViewModels;

namespace userinterface.Views
{
    public partial class ProfilesView : ReactiveUserControl<ProfilesViewModel>
    {
        public ProfilesView()
        {
            InitializeComponent();
        }

        protected override void OnInitialized()
        {
            ViewModel!.SensitivityX = this.Get<AvaPlot>(ProfilesViewModel.SensitivityXName);
            //ViewModel!.GainX        = this.Get<AvaPlot>(ProfilesViewModel.GainXName);
            //ViewModel!.VelocityX    = this.Get<AvaPlot>(ProfilesViewModel.VelocityXName);

            //ViewModel!.SensitivityY = this.Get<AvaPlot>(ProfilesViewModel.SensitivityYName);
            //ViewModel!.GainY        = this.Get<AvaPlot>(ProfilesViewModel.GainYName);
            //ViewModel!.VelocityY    = this.Get<AvaPlot>(ProfilesViewModel.VelocityYName);
        }
    }
}
