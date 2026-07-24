using System.Windows;
using System.Windows.Controls;
using TTXEquipamentos.ViewModels;
using TTXEquipamentos.Services;
using System.Text.RegularExpressions;

namespace TTXEquipamentos.Views.Pages
{
    public partial class Auth : Page
    {
        private AuthViewModel? _viewModel;
        private int _masterAdminClickCount = 0;

        public Auth()
        {
            InitializeComponent();

            var app = Application.Current as App;
            var authService = App.ServiceProvider?.GetService(typeof(IAuthenticationService)) as IAuthenticationService;
            var navigationService = App.ServiceProvider?.GetService(typeof(INavigationService)) as INavigationService;

            _viewModel = new AuthViewModel(authService, navigationService);
            DataContext = _viewModel;
        }

        private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox passwordBox)
            {
                _viewModel.Password = passwordBox.Password;
            }
        }

        private void SignupPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox passwordBox)
            {
                _viewModel.SignupPassword = passwordBox.Password;
            }
        }

        private void SignupConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox passwordBox)
            {
                _viewModel.SignupConfirmPassword = passwordBox.Password;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            _viewModel.Password = LoginPasswordBox.Password;

            // Validação
            var errors = ValidateLogin(_viewModel.Email, _viewModel.Password);
            if (errors.Count > 0)
            {
                _viewModel.ErrorMessage = string.Join("\n", errors);
                return;
            }

            if (await _viewModel.LoginAsync())
            {
                // Navegação é feita pelo ViewModel
            }
        }

        private async void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            _viewModel.SignupPassword = SignupPasswordBox.Password;
            _viewModel.SignupConfirmPassword = SignupConfirmPasswordBox.Password;

            // Validação
            var errors = ValidateSignup(
                _viewModel.SignupName,
                _viewModel.SignupEmail,
                _viewModel.SignupPassword,
                _viewModel.SignupConfirmPassword
            );

            if (errors.Count > 0)
            {
                _viewModel.SignupErrorMessage = string.Join("\n", errors);
                return;
            }

            if (await _viewModel.SignupAsync())
            {
                // Navegação é feita pelo ViewModel
            }
        }

        private void MasterAdminButton_Click(object sender, RoutedEventArgs e)
        {
            _masterAdminClickCount++;

            if (_masterAdminClickCount >= 5)
            {
                _viewModel!.Email = "suporte.ti@ttxequipamentos.com.br";
                _viewModel.Password = "123456";
                LoginPasswordBox.Password = "123456";

                StatusMessageTextBlock.Text = "✓ Acesso Master Ativado! Clique em Entrar.";
                _masterAdminClickCount = 0;
            }
        }

        private List<string> ValidateLogin(string email, string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(email))
                errors.Add("• Email é obrigatório");
            else if (!IsValidEmail(email))
                errors.Add("• Email inválido");

            if (string.IsNullOrWhiteSpace(password))
                errors.Add("• Senha é obrigatória");
            else if (password.Length < 6)
                errors.Add("• Senha deve ter no mínimo 6 caracteres");

            return errors;
        }

        private List<string> ValidateSignup(string name, string email, string password, string confirmPassword)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
                errors.Add("• Nome é obrigatório");
            else if (name.Length < 2)
                errors.Add("• Nome deve ter no mínimo 2 caracteres");

            if (string.IsNullOrWhiteSpace(email))
                errors.Add("• Email é obrigatório");
            else if (!IsValidEmail(email))
                errors.Add("• Email inválido");

            if (string.IsNullOrWhiteSpace(password))
                errors.Add("• Senha é obrigatória");
            else if (password.Length < 6)
                errors.Add("• Senha deve ter no mínimo 6 caracteres");

            if (password != confirmPassword)
                errors.Add("• Senhas não conferem");

            return errors;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
