using System.Windows.Controls;

namespace TTXEquipamentos.Views.Pages
{
    public partial class ExtraCosts : Page
    {
        public ExtraCosts()
        {
            InitializeComponent();
            var vm = App.ServiceProvider?.GetService(typeof(ViewModels.ExtraCostsViewModel));
            if (vm != null) this.DataContext = vm;
        }
    }
}