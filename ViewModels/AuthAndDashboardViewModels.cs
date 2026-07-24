using System.Windows.Input;
using TTXEquipamentos.Services;

namespace TTXEquipamentos.ViewModels
{
    public class AuthViewModel : ViewModelBase
    {
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigationService;

        private string _email = string.Empty;
        private string _password = string.Empty;
        
        private string _signupName = string.Empty;
        private string _signupEmail = string.Empty;
        private string _signupPassword = string.Empty;
        private string _signupConfirmPassword = string.Empty;
        private string _signupErrorMessage = string.Empty;
        private bool _signupDialogOpen;
        private string _signupDialogMessage = string.Empty;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string SignupName
        {
            get => _signupName;
            set => SetProperty(ref _signupName, value);
        }

        public string SignupEmail
        {
            get => _signupEmail;
            set => SetProperty(ref _signupEmail, value);
        }

        public string SignupPassword
        {
            get => _signupPassword;
            set => SetProperty(ref _signupPassword, value);
        }

        public string SignupConfirmPassword
        {
            get => _signupConfirmPassword;
            set => SetProperty(ref _signupConfirmPassword, value);
        }

        public string SignupErrorMessage
        {
            get => _signupErrorMessage;
            set => SetProperty(ref _signupErrorMessage, value);
        }

        public bool SignupDialogOpen { get => _signupDialogOpen; set => SetProperty(ref _signupDialogOpen, value); }
        public string SignupDialogMessage { get => _signupDialogMessage; set => SetProperty(ref _signupDialogMessage, value); }

        public ICommand LoginCommand { get; }
        public ICommand SignupCommand { get; }
        public ICommand CloseSignupDialogCommand { get; }
        public ICommand MasterAdminClickCommand { get; }

        public AuthViewModel(IAuthenticationService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;

            LoginCommand = new RelayCommand(_ => ExecuteLoginCommand(), _ => !IsLoading && !string.IsNullOrEmpty(Email));
            SignupCommand = new RelayCommand(_ => ExecuteSignupCommand(), _ => !IsLoading && !string.IsNullOrEmpty(SignupName));
            MasterAdminClickCommand = new RelayCommand(_ => MasterAdminClick(), _ => true);
            CloseSignupDialogCommand = new RelayCommand(_ => { SignupDialogOpen = false; SignupDialogMessage = string.Empty; }, _ => true);
        }

        private async void ExecuteLoginCommand()
        {
            if (await ExecuteAsync(async () =>
            {
                var result = await _authService.LoginAsync(Email, Password);
                if (result)
                {
                    _navigationService.NavigateToDashboard();
                }
                else
                {
                    ErrorMessage = "Email ou senha inválidos";
                }
            }))
            {
                // Success
            }
        }

        private async void ExecuteSignupCommand()
        {
            if (await ExecuteAsync(async () =>
            {
                var result = await _authService.SignupAsync(SignupName, SignupEmail, SignupPassword);
                if (result.Success)
                {
                    // Registro criado — abrir diálogo visível para aguardar aprovação do administrador
                    SignupDialogMessage = result.Error ?? "Cadastro realizado! Aguarde a aprovação do administrador para acessar o sistema.";
                    SignupDialogOpen = true;
                }
                else
                {
                    SignupErrorMessage = result.Error ?? "Erro ao criar conta";
                }
            }))
            {
                // Success
            }
        }

        public async Task<bool> LoginAsync()
        {
            bool loginResult = false;
            await ExecuteAsync(async () =>
            {
                loginResult = await _authService.LoginAsync(Email, Password);
                if (loginResult)
                {
                    _navigationService.NavigateToDashboard();
                }
                else
                {
                    ErrorMessage = "Email ou senha inválidos";
                }
            });
            return loginResult;
        }

        public async Task<bool> SignupAsync()
        {
            bool signupResult = false;
            await ExecuteAsync(async () =>
            {
                var result = await _authService.SignupAsync(SignupName, SignupEmail, SignupPassword);
                signupResult = result.Success;
                if (result.Success)
                {
                    // Don't auto-navigate; show dialog to wait for admin approval
                    SignupDialogMessage = result.Error ?? "Cadastro realizado! Aguarde a aprovação do administrador para acessar o sistema.";
                    SignupDialogOpen = true;
                }
                else
                {
                    SignupErrorMessage = result.Error ?? "Erro ao criar conta";
                }
            });
            return signupResult;
        }

        private void MasterAdminClick()
        {
            // Clique no wrench (🔧) é tratado no code-behind
        }
    }
}
