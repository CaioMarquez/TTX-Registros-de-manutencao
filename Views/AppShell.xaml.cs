using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TTXEquipamentos.Services;

namespace TTXEquipamentos.Views
{
    public partial class AppShell : UserControl
    {
        private readonly IAuthenticationService? _authService;
        private readonly INavigationService? _navigationService;
        private Button? _activeButton;

        public AppShell()
        {
            InitializeComponent();

            _authService = App.ServiceProvider?.GetService(typeof(IAuthenticationService)) as IAuthenticationService;
            _navigationService = App.ServiceProvider?.GetService(typeof(INavigationService)) as INavigationService;

            PopulateSidebar();
            UpdateUserInfo();
        }

        private void PopulateSidebar()
        {
            var userRole = _authService?.GetCurrentUserRole() ?? "tecnico";

            var menuItems = new (string icon, string label, string page, bool visible)[]
            {
                ("📊", "Dashboard", "Dashboard", true),
                ("⚙", "Máquinas", "Machines", true),
                ("📋", "Nova O.S. / Relatório", "NewOS", true),
                ("📑", "Minhas OS", "MyOS", true),
                ("📚", "Histórico", "History", true),
                ("📈", "Indicadores", "Indicators", true),
                ("💰", "Custos Extras", "ExtraCosts", userRole == "admin" || userRole == "supervisor"),
                ("👷", "Colaboradores", "Collaborators", true),
                ("👥", "Usuários", "Users", userRole == "admin"),
                ("ProfileIcon", "Perfil", "Profile", true)
            };

            foreach (var (icon, label, page, visible) in menuItems)
            {
                if (!visible) continue;

                var sp = new StackPanel { Orientation = Orientation.Horizontal };
                sp.Children.Add(CreateMenuIcon(icon));
                sp.Children.Add(new TextBlock
                {
                    Text = label,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });

                var button = new Button
                {
                    Content = sp,
                    Padding = new Thickness(16, 10, 16, 10),
                    Margin = new Thickness(0, 1, 0, 1),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0xE6, 0xD8)),
                    FontSize = 14,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    BorderThickness = new Thickness(0),
                    Tag = page
                };

                // Custom template with rounded corners
                var template = new ControlTemplate(typeof(Button));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
                borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(PaddingProperty));
                var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                borderFactory.AppendChild(contentPresenter);
                template.VisualTree = borderFactory;

                // Hover trigger
                var hoverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x15))));
                template.Triggers.Add(hoverTrigger);

                button.Template = template;

                button.Click += (s, e) =>
                {
                    NavigateToPage(page);
                    SetActiveButton(button);
                    UpdatePageTitle(label);
                };

                SidebarMenu.Children.Add(button);

                // Activate Dashboard by default
                if (page == "Dashboard")
                {
                    SetActiveButton(button);
                }
            }
        }

        private UIElement CreateMenuIcon(string icon)
        {
            if (icon == "ProfileIcon")
            {
                try
                {
                    return new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icon.png", UriKind.Absolute)),
                        Width = 16,
                        Height = 16,
                        Margin = new Thickness(0, 0, 12, 0)
                    };
                }
                catch
                {
                    // Falha ao carregar como recurso incorporado
                }
            }

            return new TextBlock
            {
                Text = icon,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
        }

        private void SetActiveButton(Button button)
        {
            // Reset previous
            if (_activeButton != null)
            {
                _activeButton.Background = Brushes.Transparent;
            }

            // Set active
            button.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x6A, 0x00)); // Orange primary
            button.Foreground = Brushes.White;
            _activeButton = button;
        }

        private void UpdatePageTitle(string title)
        {
            PageTitleTextBlock.Text = title;
        }

        private void UpdateUserInfo()
        {
            var userName = _authService?.GetCurrentUserName() ?? "Usuário";
            var userRole = _authService?.GetCurrentUserRole() ?? "tecnico";
            CurrentUserNameTextBlock.Text = userName;
            CurrentUserRoleTextBlock.Text = userRole switch
            {
                "admin" => "Administrador",
                "supervisor" => "Supervisor",
                "tecnico" => "Técnico",
                _ => userRole
            };
            HeaderUserText.Text = userName;
        }

        private void NavigateToPage(string page)
        {
            _navigationService?.NavigateToPage(page);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService?.NavigateToAuth();
        }
    }
}
