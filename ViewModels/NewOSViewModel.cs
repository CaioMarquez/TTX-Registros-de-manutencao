using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TTXEquipamentos.Models;
using TTXEquipamentos.Services;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class PartItemVM : ViewModelBase
    {
        private string _name = "";
        public string Name { get => _name; set => SetProperty(ref _name, value); }

        private int _quantity = 1;
        public int Quantity { get => _quantity; set => SetProperty(ref _quantity, value); }

        private double _unitCost;
        public double UnitCost { get => _unitCost; set { SetProperty(ref _unitCost, value); OnPropertyChanged(nameof(Total)); } }

        public double Total => Quantity * UnitCost;
    }

    public class ChecklistItemVM : ViewModelBase
    {
        public string Group { get; set; } = "";
        public int Index { get; set; }

        private string _itemName = "";
        public string ItemName { get => _itemName; set => SetProperty(ref _itemName, value); }

        private string? _status; // "ok", "nao_ok", "na", null
        public string? Status { get => _status; set { SetProperty(ref _status, value); OnPropertyChanged(nameof(IsOk)); OnPropertyChanged(nameof(IsNaoOk)); OnPropertyChanged(nameof(IsNa)); OnPropertyChanged(nameof(ShowDefect)); } }

        public bool IsOk => Status == "ok";
        public bool IsNaoOk => Status == "nao_ok";
        public bool IsNa => Status == "na";
        public bool ShowDefect => Status == "nao_ok";

        private string _defect = "";
        public string Defect { get => _defect; set => SetProperty(ref _defect, value); }

        public ICommand SetStatusCommand { get; }

        public ChecklistItemVM()
        {
            SetStatusCommand = new RelayCommand(p =>
            {
                var s = p?.ToString();
                Status = (Status == s) ? null : s;
            });
        }
    }

    public class NewOSViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navService;

        // OS Type
        private string _osType = "preventiva";
        public string OsType
        {
            get => _osType;
            set
            {
                if (SetProperty(ref _osType, value))
                {
                    OnPropertyChanged(nameof(IsPreventiva));
                    OnPropertyChanged(nameof(IsCorretiva));
                    OnPropertyChanged(nameof(IsNovaInstalacao));
                    OnPropertyChanged(nameof(ShowCorrectiveFields));
                    OnPropertyChanged(nameof(ShowPreventiveFields));
                    OnPropertyChanged(nameof(ShowMachineSelection));
                    if (IsNovaInstalacao)
                    {
                        SelectedMachine = null;
                    }
                }
            }
        }
        public bool IsPreventiva => OsType == "preventiva";
        public bool IsCorretiva => OsType == "corretiva";
        public bool IsNovaInstalacao => OsType == "instalacao";
        public bool ShowCorrectiveFields => IsCorretiva;
        public bool ShowPreventiveFields => IsPreventiva && SelectedMachine != null;
        public bool ShowMachineSelection => !IsNovaInstalacao;

        // Machine list
        public ObservableCollection<Machine> Machines { get; } = new();
        
        private ObservableCollection<Machine> _filteredMachines = new();
        public ObservableCollection<Machine> FilteredMachines
        {
            get => _filteredMachines;
            set => SetProperty(ref _filteredMachines, value);
        }

        private string _machineSearchText = "";

        public string MachineSearchText
        {
            get => _machineSearchText;
            set
            {
                if (SetProperty(ref _machineSearchText, value))
                {
                    UpdateMachineFilter();
                }
            }
        }

        private Machine? _selectedMachine;
        public Machine? SelectedMachine
        {
            get => _selectedMachine;
            set
            {
                if (SetProperty(ref _selectedMachine, value))
                {
                    if (value != null)
                    {
                        MachineArea = value.Area ?? "";
                        Nature = value.Type ?? "eletrica";

                        MachineSearchText = FormatMachineLabel(value);
                    }
                    else
                    {
                        MachineSearchText = string.Empty;
                    }

                    OnPropertyChanged(nameof(ShowMachineInfo));
                    OnPropertyChanged(nameof(ShowCorrectiveFields));
                    OnPropertyChanged(nameof(ShowPreventiveFields));
                    OnPropertyChanged(nameof(SelectedMachineLabel));
                }
            }
        }

        private string FormatMachineLabel(Machine machine)
        {
            if (!string.IsNullOrWhiteSpace(machine.Tag) && !string.IsNullOrWhiteSpace(machine.Name))
                return $"{machine.Tag} - {machine.Name}";
            return machine.Tag ?? machine.Name ?? string.Empty;
        }
        public bool ShowMachineInfo => SelectedMachine != null;

        public string SelectedMachineLabel => SelectedMachine != null ? FormatMachineLabel(SelectedMachine) : string.Empty;
        private string _machineArea = "";
        public string MachineArea { get => _machineArea; set => SetProperty(ref _machineArea, value); }

        private string _nature = "eletrica";
        public string Nature 
        { 
            get => _nature; 
            set 
            { 
                if (SetProperty(ref _nature, value))
                {
                    OnPropertyChanged(nameof(ShowNatureCustomField));
                }
            }
        }

        private string _customNature = "";
        public string CustomNature { get => _customNature; set => SetProperty(ref _customNature, value); }

        public bool ShowNatureCustomField => Nature == "outro";

        // Nova Instalação fields
        private string _installationArea = "";
        public string InstallationArea { get => _installationArea; set => SetProperty(ref _installationArea, value); }

        public ObservableCollection<string> InstallationTypes { get; } = new();

        private string _selectedInstallationType = "eletrica";
        public string SelectedInstallationType { get => _selectedInstallationType; set => SetProperty(ref _selectedInstallationType, value); }

        // Corrective fields
        private DateTime _startDate = DateTime.Today;
        public DateTime StartDate 
        { 
            get => _startDate; 
            set 
            { 
                if (SetProperty(ref _startDate, value))
                {
                    // Se o usuário não alterou a data de término (EndDate == valor anterior de StartDate), duplicar a nova data
                    if (_endDate == _startDate || !_userModifiedEndDate)
                    {
                        _endDate = value;
                        _userModifiedEndDate = false;
                        OnPropertyChanged(nameof(EndDate));
                    }
                }
            }
        }

        private string _startTime = "";
        public string StartTime { get => _startTime; set => SetProperty(ref _startTime, value); }

        private DateTime _endDate = DateTime.Today;
        private bool _userModifiedEndDate = false;
        public DateTime EndDate 
        { 
            get => _endDate; 
            set 
            { 
                if (SetProperty(ref _endDate, value))
                {
                    // Marcar que o usuário modificou a data de término (apenas se for diferente de StartDate)
                    _userModifiedEndDate = (value != _startDate);
                }
            }
        }

        private string _endTime = "";
        public string EndTime { get => _endTime; set => SetProperty(ref _endTime, value); }

        private string _requester = "";
        public string RequesterField { get => _requester; set => SetProperty(ref _requester, value); }

        private string _description = "";
        public string Description { get => _description; set => SetProperty(ref _description, value); }

        private string _causeRoot = "";
        public string CauseRoot { get => _causeRoot; set => SetProperty(ref _causeRoot, value); }

        private string _workDescription = "";
        public string WorkDescription { get => _workDescription; set => SetProperty(ref _workDescription, value); }

        // Relatório Fields
        private int _reportNumber;
        public int ReportNumber { get => _reportNumber; set => SetProperty(ref _reportNumber, value); }

        private string? _weatherMorning;
        public string? WeatherMorning { get => _weatherMorning; set => SetProperty(ref _weatherMorning, value); }

        private string? _weatherAfternoon;
        public string? WeatherAfternoon { get => _weatherAfternoon; set => SetProperty(ref _weatherAfternoon, value); }

        private string? _weatherNight;
        public string? WeatherNight { get => _weatherNight; set => SetProperty(ref _weatherNight, value); }

        private string? _rainfallIndex;
        public string? RainfallIndex { get => _rainfallIndex; set => SetProperty(ref _rainfallIndex, value); }

        // New Report Item Inputs
        private string? _newLaborItem;
        public string? NewLaborItem { get => _newLaborItem; set => SetProperty(ref _newLaborItem, value); }
        private string? _newEquipmentItem;
        public string? NewEquipmentItem { get => _newEquipmentItem; set => SetProperty(ref _newEquipmentItem, value); }
        private string? _newActivityItem;
        public string? NewActivityItem { get => _newActivityItem; set => SetProperty(ref _newActivityItem, value); }
        private string? _newIncidentItem;
        public string? NewIncidentItem { get => _newIncidentItem; set => SetProperty(ref _newIncidentItem, value); }
        private string? _newCommentItem;
        public string? NewCommentItem { get => _newCommentItem; set => SetProperty(ref _newCommentItem, value); }

        // Report Collections
        public ObservableCollection<MaintenanceReportItem> LaborForce { get; } = new();
        public ObservableCollection<MaintenanceReportItem> EquipmentItems { get; } = new();
        public ObservableCollection<MaintenanceReportItem> Activities { get; } = new();
        public ObservableCollection<MaintenanceReportItem> Incidents { get; } = new();
        public ObservableCollection<MaintenanceReportItem> Comments { get; } = new();
        public ObservableCollection<MaintenanceReportPhoto> Photos { get; } = new();

        public ObservableCollection<Collaborator> LaborUsers { get; } = new();

        private Collaborator? _selectedLaborUser;
        public Collaborator? SelectedLaborUser
        {
            get => _selectedLaborUser;
            set => SetProperty(ref _selectedLaborUser, value);
        }

        public ICommand AddLaborCommand { get; }
        public ICommand AddEquipmentCommand { get; }
        public ICommand AddActivityCommand { get; }
        public ICommand AddIncidentCommand { get; }
        public ICommand AddCommentCommand { get; }
        public ICommand RemoveReportItemCommand { get; }
        public ICommand RemovePhotoCommand { get; }

        // Parts
        public ObservableCollection<PartItemVM> Parts { get; } = new();

        public double PartsTotal => Parts.Sum(p => p.Quantity * p.UnitCost);
        public bool HasParts => Parts.Count > 0;

        // Commands
        public ICommand SetPreventivaCommand { get; }
        public ICommand SetCorretivaCommand { get; }
        public ICommand SetNovaInstalacaoCommand { get; }
        public ICommand AddPartCommand { get; }
        public ICommand RemovePartCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public NewOSViewModel(ILocalDatabaseService databaseService, IAuthenticationService authService, INavigationService navService)
        {
            _databaseService = databaseService;
            _authService = authService;
            _navService = navService;

            SetPreventivaCommand = new RelayCommand(_ => OsType = "preventiva");
            SetNovaInstalacaoCommand = new RelayCommand(_ => OsType = "instalacao");
            SetCorretivaCommand = new RelayCommand(_ => OsType = "corretiva");
            AddPartCommand = new RelayCommand(_ => AddPart());
            RemovePartCommand = new RelayCommand(p => RemovePart(p as PartItemVM));
            SaveCommand = new RelayCommand(async _ => await SaveOSAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => _navService.NavigateToPage("Dashboard"));

            AddLaborCommand = new RelayCommand(_ => 
            {
                if (SelectedLaborUser != null)
                {
                    LaborForce.Add(new MaintenanceReportItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Description = SelectedLaborUser.Function is string function && !string.IsNullOrWhiteSpace(function)
                            ? $"{SelectedLaborUser.Name} ({function})"
                            : SelectedLaborUser.Name ?? "Colaborador",
                        CreatedAt = DateTime.Now
                    });
                    SelectedLaborUser = null;
                }
                else if (!string.IsNullOrWhiteSpace(NewLaborItem))
                {
                    LaborForce.Add(new MaintenanceReportItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Description = NewLaborItem,
                        CreatedAt = DateTime.Now
                    });
                    NewLaborItem = "";
                }
            });
            AddEquipmentCommand = new RelayCommand(_ => { if (!string.IsNullOrWhiteSpace(NewEquipmentItem)) { EquipmentItems.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewEquipmentItem, CreatedAt = DateTime.Now }); NewEquipmentItem = ""; } });
            AddActivityCommand = new RelayCommand(_ => { if (!string.IsNullOrWhiteSpace(NewActivityItem)) { Activities.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewActivityItem, CreatedAt = DateTime.Now }); NewActivityItem = ""; } });
            AddIncidentCommand = new RelayCommand(_ => { if (!string.IsNullOrWhiteSpace(NewIncidentItem)) { Incidents.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewIncidentItem, CreatedAt = DateTime.Now }); NewIncidentItem = ""; } });
            AddCommentCommand = new RelayCommand(_ => { if (!string.IsNullOrWhiteSpace(NewCommentItem)) { Comments.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewCommentItem, CreatedAt = DateTime.Now }); NewCommentItem = ""; } });
            
            RemoveReportItemCommand = new RelayCommand(param => 
            {
                if (param is MaintenanceReportItem item)
                {
                    LaborForce.Remove(item);
                    EquipmentItems.Remove(item);
                    Activities.Remove(item);
                    Incidents.Remove(item);
                    Comments.Remove(item);
                }
            });
            
            RemovePhotoCommand = new RelayCommand(param => 
            {
                if (param is MaintenanceReportPhoto photo) Photos.Remove(photo);
            });

            ReportNumber = GenerateReportNumber();
            _ = LoadMachinesAsync();
            _ = LoadLaborUsersAsync();

            // Installation types for Nova Instalação
            InstallationTypes.Add("eletrica");
            InstallationTypes.Add("mecanica");
            InstallationTypes.Add("infraestrutura");
            InstallationTypes.Add("rede");
        }

        // Exposed for automated self-test runs from App startup
        public async Task SelfTestFilterAsync()
        {
            await LoadMachinesAsync();
        }

        private int GenerateReportNumber()
        {
            return int.Parse(DateTime.Now.ToString("yyyyMMdd")) % 100 + new Random().Next(1, 100);
        }

        private async Task LoadMachinesAsync()
        {
            var m = await _databaseService.GetAllAsync<Machine>("machines");
            foreach (var item in m.OrderBy(x => x.Name))
                Machines.Add(item);
            Serilog.Log.Information("[NewOSViewModel] Loaded {Count} machines", Machines.Count);
            UpdateMachineFilter();

            // Self-test helper: if launched with --selftest-filter, briefly set MachineSearchText
            try
            {
                var args = Environment.GetCommandLineArgs();
                if (args != null && args.Contains("--selftest-filter"))
                {
                    Serilog.Log.Information("[NewOSViewModel] Self-test: applying temporary MachineSearchText=MQ-");
                    MachineSearchText = "MQ-";
                    // wait a moment to let UI update and filter
                    await Task.Delay(500);
                    Serilog.Log.Information("[NewOSViewModel] Self-test: results after setting search='{Search}': {Count}", MachineSearchText, FilteredMachines.Count);
                    // clear test
                    MachineSearchText = "";
                    UpdateMachineFilter();
                }
            }
            catch { }
        }

        private void UpdateMachineFilter()
        {
            if (string.IsNullOrWhiteSpace(MachineSearchText))
            {
                // Se vazio, mostra todas
                FilteredMachines.Clear();
                foreach (var machine in Machines)
                    FilteredMachines.Add(machine);
            }
            else
            {
                // Filtra por Tag ou Name que começam com o texto
                var filtered = Machines.Where(m =>
                    (m.Tag?.StartsWith(MachineSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (m.Name?.StartsWith(MachineSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    FormatMachineLabel(m).StartsWith(MachineSearchText, StringComparison.OrdinalIgnoreCase)
                ).OrderBy(x => x.Tag).ThenBy(x => x.Name);

                FilteredMachines.Clear();
                foreach (var machine in filtered)
                    FilteredMachines.Add(machine);
            }
            Serilog.Log.Information("[NewOSViewModel] UpdateMachineFilter search='{Search}' results={Count}", MachineSearchText, FilteredMachines.Count);
        }

        private async Task LoadLaborUsersAsync()
        {
            var users = await _databaseService.GetAllAsync<Collaborator>("collaborators");
            foreach (var item in users.OrderBy(x => x.Name))
                LaborUsers.Add(item);
        }

        private void AddPart()
        {
            var p = new PartItemVM();
            p.PropertyChanged += (_, __) => { OnPropertyChanged(nameof(PartsTotal)); OnPropertyChanged(nameof(HasParts)); };
            Parts.Add(p);
            OnPropertyChanged(nameof(HasParts));
            OnPropertyChanged(nameof(PartsTotal));
        }

        private void RemovePart(PartItemVM? part)
        {
            if (part != null) Parts.Remove(part);
            OnPropertyChanged(nameof(HasParts));
            OnPropertyChanged(nameof(PartsTotal));
        }

        private bool CanSave()
        {
            // Botão sempre habilitado - validações ocorrem ao clicar
            return true;
        }

        /// <summary>
        /// Parses time string "HH:mm" or "HHmm" and combines with date into DateTime
        /// </summary>
        private DateTime? ParseTimeWithDate(string timeStr, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(timeStr))
                return null;

            timeStr = timeStr.Replace(":", "").Trim();
            if (timeStr.Length != 4 || !int.TryParse(timeStr, out var timeNum))
                return null;

            int hours = timeNum / 100;
            int minutes = timeNum % 100;

            if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
                return null;

            return date.Date.AddHours(hours).AddMinutes(minutes);
        }

        private async Task SaveOSAsync()
        {
            var now = DateTime.Now;

            // ========== VALIDAÇÕES DE CAMPOS OBRIGATÓRIOS ==========
            
            // 1. Validar Data e Horários (aba Informações)
            if (string.IsNullOrWhiteSpace(StartTime))
            {
                MessageBox.Show("⚠️ Campo obrigatório: Digite a hora de início (ex: 14:30)", "Campos Obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(EndTime))
            {
                MessageBox.Show("⚠️ Campo obrigatório: Digite a hora de fim (ex: 16:45)", "Campos Obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validar Solicitante (aba Informações)
            if (string.IsNullOrWhiteSpace(RequesterField))
            {
                MessageBox.Show("⚠️ Campo obrigatório: Digite o nome do solicitante", "Campos Obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2.5. Validar Natureza Customizada (se "outro" for selecionado)
            if (Nature == "outro" && string.IsNullOrWhiteSpace(CustomNature))
            {
                MessageBox.Show("⚠️ Campo obrigatório: Digite a natureza customizada", "Campos Obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Validar Nova Instalação (se selecionada)
            if (IsNovaInstalacao)
            {
                if (string.IsNullOrWhiteSpace(InstallationArea))
                {
                    MessageBox.Show("⚠️ Campo obrigatório: Digite a área da nova instalação", "Campos Obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                // 4. Validar Equipamento (se não for Nova Instalação)
                if (SelectedMachine == null)
                {
                    MessageBox.Show("⚠️ Campo obrigatório: Selecione um equipamento/máquina", "Campos Obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // 5. Validar Trabalho Executado (apenas corretiva e nova instalação)
            if (!IsPreventiva && Activities.Count == 0 && string.IsNullOrWhiteSpace(NewActivityItem))
            {
                MessageBox.Show("⚠️ Campo obrigatório: Adicione pelo menos um trabalho executado", "Campos Obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar e parsear horas
            DateTime? startDateTime = ParseTimeWithDate(StartTime, StartDate);
            DateTime? endDateTime = ParseTimeWithDate(EndTime, EndDate);

            if (!startDateTime.HasValue)
            {
                MessageBox.Show("Hora de início inválida. Use formato HH:mm (ex: 14:30)", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!endDateTime.HasValue)
            {
                MessageBox.Show("Hora de fim inválida. Use formato HH:mm (ex: 16:45)", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var record = new MaintenanceRecord
            {
                Id = Guid.NewGuid().ToString(),
                ReportNumber = ReportNumber,
                Type = IsPreventiva ? MaintenanceType.preventiva : IsNovaInstalacao ? MaintenanceType.instalacao : MaintenanceType.corretiva,
                Nature = Nature == "mecanica" ? MaintenanceNature.mecanica : Nature == "outro" ? MaintenanceNature.outro : MaintenanceNature.eletrica,
                CustomNature = Nature == "outro" ? CustomNature : null,
                MachineId = SelectedMachine?.Id,
                MachineTag = SelectedMachine?.Tag,
                TechnicianId = _authService.GetCurrentUserId(),
                TechnicianName = _authService.GetCurrentUserName(),
                StartTime = startDateTime ?? now,
                EndTime = endDateTime,
                WeatherMorning = WeatherMorning,
                WeatherAfternoon = WeatherAfternoon,
                WeatherNight = WeatherNight,
                RainfallIndex = RainfallIndex,
                Description = IsCorretiva ? Description : null,
                RootCause = IsCorretiva ? CauseRoot : null,
                WorkDescription = IsCorretiva ? WorkDescription : null,
                Observations = IsNovaInstalacao ? SelectedInstallationType : null,
                Area = IsNovaInstalacao ? InstallationArea : SelectedMachine?.Area,
                Requester = RequesterField,
                Status = "finalizada",
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var item in LaborForce) record.LaborForce.Add(item);
            foreach (var item in EquipmentItems) record.EquipmentItems.Add(item);
            
            if (!string.IsNullOrWhiteSpace(NewActivityItem))
                Activities.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewActivityItem, CreatedAt = now });
            foreach (var item in Activities) record.Activities.Add(item);
            
            if (!string.IsNullOrWhiteSpace(NewIncidentItem))
                Incidents.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewIncidentItem, CreatedAt = now });
            foreach (var item in Incidents) record.Incidents.Add(item);
            
            if (!string.IsNullOrWhiteSpace(NewCommentItem))
                Comments.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewCommentItem, CreatedAt = now });
            foreach (var item in Comments) record.Comments.Add(item);
            
            foreach (var photo in Photos) record.Photos.Add(photo);

            // Parts for corretiva
            if (IsCorretiva && Parts.Count > 0)
            {
                foreach (var p in Parts.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
                {
                    record.Items.Add(new MaintenanceItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        MaintenanceRecordId = record.Id,
                        Description = p.Name,
                        Quantity = p.Quantity,
                        UnitCost = p.UnitCost,
                        CreatedAt = now
                    });
                }
                record.TotalCost = Parts.Sum(p => p.Quantity * p.UnitCost);
            }

            // DEBUG: Log antes de salvar
            Console.WriteLine($"DEBUG SaveOSAsync - LaborForce items: {record.LaborForce.Count}");
            Console.WriteLine($"DEBUG SaveOSAsync - Activities items: {record.Activities.Count}");
            Console.WriteLine($"DEBUG SaveOSAsync - Photos items: {record.Photos.Count}");
            
            var recordJson = System.Text.Json.JsonSerializer.Serialize(record, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"DEBUG SaveOSAsync - Serialized record JSON:\n{recordJson}");

            var success = await _databaseService.SaveAsync("maintenance_records", record);
            if (success)
            {
                MessageBox.Show("Ordem de Serviço criada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                _navService.NavigateToPage("MyOS");
            }
            else
            {
                MessageBox.Show("Erro ao criar OS.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
