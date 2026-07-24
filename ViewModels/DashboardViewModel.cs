using System.Windows.Input;
using TTXEquipamentos.Models;
using TTXEquipamentos.Services;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IMaintenanceCalculationsService _calculationsService;
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigationService;
        private readonly ILocalDatabaseService _databaseService;

        // KPI fields
        private int _preventiveCount;
        private int _correctiveCount;
        private int _machineCount;
        private int _totalMonth;
        private double _extraCosts;

        // Welcome fields
        private string _greeting = "";
        private string _userName = "";
        private string _roleDescription = "";
        private string _userRole = "";
        private string _currentTime = "";

        // KPI properties
        public int PreventiveCount { get => _preventiveCount; set => SetProperty(ref _preventiveCount, value); }
        public int CorrectiveCount { get => _correctiveCount; set => SetProperty(ref _correctiveCount, value); }
        public int MachineCount { get => _machineCount; set => SetProperty(ref _machineCount, value); }
        public int TotalMonth { get => _totalMonth; set => SetProperty(ref _totalMonth, value); }
        public double ExtraCosts { get => _extraCosts; set => SetProperty(ref _extraCosts, value); }
        public string ExtraCostsFormatted => $"R$ {ExtraCosts:N2}";

        // Welcome properties
        public string Greeting { get => _greeting; set => SetProperty(ref _greeting, value); }
        public string UserName { get => _userName; set => SetProperty(ref _userName, value); }
        public string RoleDescription { get => _roleDescription; set => SetProperty(ref _roleDescription, value); }
        public string UserRole { get => _userRole; set => SetProperty(ref _userRole, value); }
        public string CurrentTime { get => _currentTime; set => SetProperty(ref _currentTime, value); }

        // Visibility for Quick Actions (based on role)
        public bool ShowNewOS => UserRole == "admin" || UserRole == "tecnico";
        public bool ShowIndicators => UserRole == "admin" || UserRole == "supervisor";
        public bool ShowMachines => UserRole == "admin";

        // Navigation commands
        public ICommand NavigateToNewOSCommand { get; }
        public ICommand NavigateToIndicatorsCommand { get; }
        public ICommand NavigateToMachinesCommand { get; }

        public DashboardViewModel(
            IMaintenanceCalculationsService calculationsService,
            IAuthenticationService authService,
            INavigationService navigationService,
            ILocalDatabaseService databaseService)
        {
            _calculationsService = calculationsService;
            _authService = authService;
            _navigationService = navigationService;
            _databaseService = databaseService;

            NavigateToNewOSCommand = new RelayCommand(_ => _navigationService.NavigateToPage("NewOS"));
            NavigateToIndicatorsCommand = new RelayCommand(_ => _navigationService.NavigateToPage("Indicators"));
            NavigateToMachinesCommand = new RelayCommand(_ => _navigationService.NavigateToPage("Machines"));
            SetGreeting();
            _ = LoadDataAsync();
        }

        private void SetGreeting()
        {
            var hour = DateTime.Now.Hour;
            if (hour < 12)
                Greeting = "Bom dia";
            else if (hour < 18)
                Greeting = "Boa tarde";
            else
                Greeting = "Boa noite";

            var fullName = _authService?.GetCurrentUserName() ?? "Usuário";
            UserName = fullName.Split(' ')[0]; // First name only

            UserRole = _authService?.GetCurrentUserRole() ?? "tecnico";
            RoleDescription = UserRole switch
            {
                "admin" => "Você tem acesso total ao sistema, incluindo gestão de máquinas e usuários.",
                "supervisor" => "Você pode visualizar dashboards, indicadores e todo o histórico de manutenções.",
                "tecnico" => "Você pode criar e gerenciar suas ordens de serviço.",
                _ => ""
            };

            CurrentTime = DateTime.Now.ToString("HH:mm:ss");

            OnPropertyChanged(nameof(ShowNewOS));
            OnPropertyChanged(nameof(ShowIndicators));
            OnPropertyChanged(nameof(ShowMachines));
        }

        public async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                var records = await _databaseService.GetAllAsync<MaintenanceRecord>("maintenance_records");
                var machines = await _databaseService.GetAllAsync<Machine>("machines");

                MachineCount = machines.Count;

                // Filter records for current month
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var monthRecords = records.Where(r => r.StartTime >= startOfMonth).ToList();

                PreventiveCount = monthRecords.Count(r => r.Type == MaintenanceType.preventiva);
                CorrectiveCount = monthRecords.Count(r => r.Type == MaintenanceType.corretiva);
                TotalMonth = PreventiveCount + CorrectiveCount;

                // Extra costs for current year
                try
                {
                    var extraCostsList = await _databaseService.GetAllAsync<ExtraCost>("extra_costs");
                    ExtraCosts = extraCostsList
                        .Where(ec => ec.InvoiceDate.Year == now.Year)
                        .Sum(ec => ec.Amount);
                }
                catch
                {
                    ExtraCosts = 0;
                }

                OnPropertyChanged(nameof(ExtraCostsFormatted));
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            });
        }
    }
}
