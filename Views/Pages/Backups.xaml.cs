using System.Windows.Controls;

namespace TTXEquipamentos.Views.Pages
{
    public partial class Backups : Page
    {
        public Backups()
        {
            InitializeComponent();
            var vm = App.ServiceProvider?.GetService(typeof(ViewModels.BackupsViewModel));
            if (vm != null) this.DataContext = vm;
        }
    }
}