using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TTXEquipamentos.Services;
using TTXEquipamentos.ViewModels;

namespace TTXEquipamentos.Views.Pages
{
    public partial class Machines : Page
    {
        public Machines()
        {
            InitializeComponent();
            var databaseService = App.ServiceProvider?.GetService(typeof(ILocalDatabaseService)) as ILocalDatabaseService;
            DataContext = new MachinesViewModel(databaseService!);

            // Fix: Redirect DataGrid scroll to parent ScrollViewer
            MachinesDataGrid.PreviewMouseWheel += DataGrid_PreviewMouseWheel;
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Prevent DataGrid from handling the scroll, bubble it up
            e.Handled = true;

            var scrollViewer = FindParentScrollViewer(sender as DependencyObject);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
            }
        }

        private ScrollViewer? FindParentScrollViewer(DependencyObject? child)
        {
            while (child != null)
            {
                child = VisualTreeHelper.GetParent(child);
                if (child is ScrollViewer sv)
                    return sv;
            }
            return null;
        }
    }
}