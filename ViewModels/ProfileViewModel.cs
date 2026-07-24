using System;
using System.Threading.Tasks;
using System.Windows.Input;
using TTXEquipamentos.Services;
using System.Collections.ObjectModel;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly IAuthenticationService _authService;
        private string _userName = string.Empty;
        private string _userEmail = string.Empty;
        private string _userRole = string.Empty;

        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _isChangingPassword;
        private ObservableCollection<string> _permissions = new();

        public string UserName { get => _userName; set => SetProperty(ref _userName, value); }
        public string UserEmail { get => _userEmail; set => SetProperty(ref _userEmail, value); }
        public string UserRole { get => _userRole; set => SetProperty(ref _userRole, value); }
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
        public bool IsChangingPassword { get => _isChangingPassword; set => SetProperty(ref _isChangingPassword, value); }
        public ObservableCollection<string> Permissions { get => _permissions; set => SetProperty(ref _permissions, value); }

        public ICommand ChangePasswordCommand { get; }

        public ProfileViewModel(IAuthenticationService authService)
        {
            _authService = authService;
            ChangePasswordCommand = new RelayCommand(async _ => await ChangePasswordAsync());
            LoadProfile();
        }

        private void LoadProfile()
        {
            UserName = _authService.GetCurrentUserName() ?? "—";
            UserEmail = _authService.GetCurrentUserEmail() ?? "—";
            var role = _authService.GetCurrentUserRole();
            
            UserRole = role switch
            {
                "admin" => "Administrador",
                "supervisor" => "Supervisor",
                "tecnico" => "Técnico",
                _ => "Sem Acesso"
            };

            Permissions.Clear();
            if (role == "admin")
            {
                Permissions.Add("Acesso total ao sistema");
                Permissions.Add("Cadastrar e gerenciar máquinas");
                Permissions.Add("Gerenciar níveis de acesso dos usuários");
                Permissions.Add("Visualizar dashboards e histórico completo");
                Permissions.Add("Criar ordens de serviço");
            }
            else if (role == "supervisor")
            {
                Permissions.Add("Visualizar dashboards e indicadores");
                Permissions.Add("Consultar histórico completo de manutenções");
                Permissions.Add("Exportar relatórios");
            }
            else if (role == "tecnico")
            {
                Permissions.Add("Criar ordens de serviço preventivas");
                Permissions.Add("Criar ordens de serviço corretivas");
                Permissions.Add("Visualizar suas próprias OS");
            }
        }

        private async Task ChangePasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                System.Windows.MessageBox.Show("A senha deve ter no mínimo 6 caracteres.", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                System.Windows.MessageBox.Show("As senhas não conferem.", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            IsChangingPassword = true;
            try
            {
                var result = await _authService.UpdatePasswordAsync(NewPassword);
                if (result.Success)
                {
                    System.Windows.MessageBox.Show("Sua senha foi atualizada com sucesso.", "Senha Alterada", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                }
                else
                {
                    System.Windows.MessageBox.Show($"Erro ao alterar senha: {result.Error}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            finally
            {
                IsChangingPassword = false;
            }
        }
    }
}
