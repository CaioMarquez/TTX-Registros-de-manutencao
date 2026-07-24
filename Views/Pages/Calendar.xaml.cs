using System.Windows.Controls;

namespace TTXEquipamentos.Views.Pages
{
    public partial class Calendar : Page
    {
        public Calendar()
        {
            InitializeComponent();
            var vm = App.ServiceProvider?.GetService(typeof(ViewModels.CalendarViewModel));
            if (vm != null) this.DataContext = vm;
        }
    }
}