using System.Windows.Controls;

namespace TTXEquipamentos.Views.Pages
{
    public partial class Profile : Page
    {
        public Profile()
        {
            InitializeComponent();
            var vm = App.ServiceProvider?.GetService(typeof(ViewModels.ProfileViewModel));
            if (vm != null) this.DataContext = vm;
        }
    }
}