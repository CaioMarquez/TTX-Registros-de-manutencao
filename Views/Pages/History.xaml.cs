using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;

namespace TTXEquipamentos.Views.Pages
{
    public partial class History : Page
    {
        public History()
        {
            InitializeComponent();
            var vm = App.ServiceProvider?.GetService(typeof(ViewModels.HistoryViewModel));
            if (vm != null) this.DataContext = vm;
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = GetParentOfType<ScrollViewer>((DependencyObject)sender);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private static T GetParentOfType<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentDepObj = child;
            do
            {
                parentDepObj = VisualTreeHelper.GetParent(parentDepObj);
                if (parentDepObj is T parent) return parent;
            }
            while (parentDepObj != null);
            return null;
        }

        private void OpenPhoto(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Models.MaintenanceReportPhoto photo && !string.IsNullOrWhiteSpace(photo.FilePath))
            {
                try
                {
                    var psi = new ProcessStartInfo(photo.FilePath) { UseShellExecute = true };
                    Process.Start(psi);
                }
                catch { }
            }
        }
}
}