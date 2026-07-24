using System.Windows;
using System.Windows.Navigation;
using System.Windows.Controls;
using TTXEquipamentos.Views.Pages;

namespace TTXEquipamentos.Services
{
    public class NavigationService : INavigationService
    {
        private Frame? _navigationFrame;

        public void SetNavigationFrame(Frame frame)
        {
            _navigationFrame = frame;
        }

        public void NavigateToPage(string pageName, object? parameter = null)
        {
            if (_navigationFrame != null)
            {
                string pageUri = $"Views/Pages/{pageName}.xaml";
                _navigationFrame.Navigate(new Uri(pageUri, UriKind.Relative));
            }
        }

        public void GoBack()
        {
            if (_navigationFrame != null && _navigationFrame.CanGoBack)
            {
                _navigationFrame.GoBack();
            }
        }

        public void NavigateToAuth()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new Uri("Views/Pages/Auth.xaml", UriKind.Relative));
                _navigationFrame = mainWindow.MainFrame;
            }
        }

        public void NavigateToDashboard()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                var appShell = new TTXEquipamentos.Views.AppShell();
                mainWindow.MainFrame.Navigate(appShell);
                
                _navigationFrame = appShell.MainContentFrame;
                NavigateToPage("Dashboard");
            }
        }
    }
}
