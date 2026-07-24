using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TTXEquipamentos.ViewModels;

namespace TTXEquipamentos.Views.Pages
{
    public partial class Users : Page
    {
        private UsersViewModel? _viewModel;

        public Users()
        {
            InitializeComponent();
            var vm = App.ServiceProvider?.GetService(typeof(ViewModels.UsersViewModel));
            if (vm != null)
            {
                _viewModel = vm as UsersViewModel;
                this.DataContext = _viewModel;

                // Subscribe to property changes
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(UsersViewModel.IsRoleDialogOpen))
                        {
                            RoleDialogOverlay.Visibility = _viewModel.IsRoleDialogOpen ? Visibility.Visible : Visibility.Collapsed;
                            RoleDialogBorder.Visibility = _viewModel.IsRoleDialogOpen ? Visibility.Visible : Visibility.Collapsed;
                        }
                        else if (e.PropertyName == nameof(UsersViewModel.IsPasswordDialogOpen))
                        {
                            RoleDialogOverlay.Visibility = _viewModel.IsPasswordDialogOpen ? Visibility.Visible : Visibility.Collapsed;
                            PasswordDialogBorder.Visibility = _viewModel.IsPasswordDialogOpen ? Visibility.Visible : Visibility.Collapsed;
                        }
                        else if (e.PropertyName == nameof(UsersViewModel.IsDeleteDialogOpen))
                        {
                            RoleDialogOverlay.Visibility = _viewModel.IsDeleteDialogOpen ? Visibility.Visible : Visibility.Collapsed;
                            DeleteDialogBorder.Visibility = _viewModel.IsDeleteDialogOpen ? Visibility.Visible : Visibility.Collapsed;
                        }
                        else if (e.PropertyName == nameof(UsersViewModel.IsApprovingUser))
                        {
                            RoleDialogSubmitBtn.Content = _viewModel.IsApprovingUser ? "Aprovar" : "Salvar";
                        }
                    };
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure initial state
            RoleDialogOverlay.Visibility = Visibility.Collapsed;
            RoleDialogBorder.Visibility = Visibility.Collapsed;
            PasswordDialogBorder.Visibility = Visibility.Collapsed;
            DeleteDialogBorder.Visibility = Visibility.Collapsed;
        }

        private void TabApproved_Click(object sender, MouseButtonEventArgs e)
        {
            ApprovedContent.Visibility = Visibility.Visible;
            PendingContent.Visibility = Visibility.Collapsed;

            // Update tab styling
            Tab_Approved.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            Tab_Approved.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
            Tab_Pending.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"));
            Tab_Pending.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"));
        }

        private void TabPending_Click(object sender, MouseButtonEventArgs e)
        {
            ApprovedContent.Visibility = Visibility.Collapsed;
            PendingContent.Visibility = Visibility.Visible;

            // Update tab styling
            Tab_Pending.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            Tab_Pending.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
            Tab_Approved.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"));
            Tab_Approved.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"));
        }
    }
}