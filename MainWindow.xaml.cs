using System.Windows;
using TTXEquipamentos.Views.Pages;
using TTXEquipamentos.Services;

namespace TTXEquipamentos
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            var navigationService = App.ServiceProvider?.GetService(typeof(INavigationService)) as NavigationService;
            
            if (navigationService != null)
            {
                navigationService.SetNavigationFrame(this.MainFrame);
            }

            // Navigate to Auth page on startup
            MainFrame.Navigate(new Uri("Views/Pages/Auth.xaml", UriKind.Relative));
        }
    }
}
