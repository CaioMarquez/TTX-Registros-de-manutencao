using System;
using System.Threading.Tasks;
using System.Windows.Input;
using TTXEquipamentos.Services;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class AlertSettingsViewModel : ViewModelBase
    {
        private readonly IAlertConfigurationService _alertService;
        private readonly IAuthenticationService _authService;

        private bool _alertsEnabled;
        private string _emailRecipient = string.Empty;
        private string _alertFrequency = "diario";
        private string _statusMessage = string.Empty;

        public bool AlertsEnabled { get => _alertsEnabled; set => SetProperty(ref _alertsEnabled, value); }
        public string EmailRecipient { get => _emailRecipient; set => SetProperty(ref _emailRecipient, value); }
        public string AlertFrequency { get => _alertFrequency; set => SetProperty(ref _alertFrequency, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand SaveCommand { get; }

        public AlertSettingsViewModel(IAlertConfigurationService alertService, IAuthenticationService authService)
        {
            _alertService = alertService;
            _authService = authService;
            SaveCommand = new RelayCommand(async (_) => await SaveSettingsAsync());
            _ = LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            var userId = _authService.GetCurrentUserId() ?? "user_1";
            var settings = await _alertService.GetAlertSettingsAsync(userId);
            if (settings != null)
            {
                if (settings.TryGetValue("alerts_enabled", out var enabled))
                    AlertsEnabled = Convert.ToBoolean(enabled);
                if (settings.TryGetValue("email_recipient", out var email))
                    EmailRecipient = email?.ToString() ?? string.Empty;
                if (settings.TryGetValue("alert_frequency", out var freq))
                    AlertFrequency = freq?.ToString() ?? "diario";
            }
        }

        private async Task SaveSettingsAsync()
        {
            var userId = _authService.GetCurrentUserId() ?? "user_1";
            var settings = new System.Collections.Generic.Dictionary<string, object>
            {
                ["alerts_enabled"] = AlertsEnabled,
                ["email_recipient"] = EmailRecipient,
                ["alert_frequency"] = AlertFrequency
            };
            var success = await _alertService.SaveAlertSettingsAsync(userId, settings);
            StatusMessage = success ? "✓ Configurações salvas com sucesso!" : "✗ Erro ao salvar configurações.";
        }
    }
}
