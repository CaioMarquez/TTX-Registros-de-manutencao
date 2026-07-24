using System.Windows.Controls;

namespace TTXEquipamentos.Views.Pages
{
    public partial class AlertSettings : Page
    {
        public AlertSettings()
        {
            InitializeComponent();
            var vm = App.ServiceProvider?.GetService(typeof(ViewModels.AlertSettingsViewModel));
            if (vm != null) this.DataContext = vm;
        }
    }
}