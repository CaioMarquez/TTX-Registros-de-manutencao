using System.Windows;
using System.Windows.Controls;
using TTXEquipamentos.ViewModels;
using TTXEquipamentos.Services;

namespace TTXEquipamentos.Views.Pages
{
    public partial class Dashboard : Page
    {
        public Dashboard()
        {
            InitializeComponent();

            var calculationsService = App.ServiceProvider?.GetService(typeof(IMaintenanceCalculationsService)) as IMaintenanceCalculationsService;
            var authService = App.ServiceProvider?.GetService(typeof(IAuthenticationService)) as IAuthenticationService;
            var navigationService = App.ServiceProvider?.GetService(typeof(INavigationService)) as INavigationService;
            var databaseService = App.ServiceProvider?.GetService(typeof(ILocalDatabaseService)) as ILocalDatabaseService;

            var viewModel = new DashboardViewModel(calculationsService!, authService!, navigationService!, databaseService!);
            DataContext = viewModel;
        }
    }
}
