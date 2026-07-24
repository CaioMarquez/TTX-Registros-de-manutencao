using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using TTXEquipamentos.Models;
using TTXEquipamentos.Services;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class HistoryViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;
        private List<MaintenanceRecord> _allRecords = new();
        private ObservableCollection<MaintenanceRecord> _records = new();
        private ICollectionView _recordsView;
        private string _searchText = string.Empty;
        private string _typeFilter = "Todos";
        private DateTime? _startDate;
        private DateTime? _endDate;
        
        // Paginação
        private const int PageSize = 15;
        private int _currentPage = 1;
        private int _totalRecords = 0;
        
        // Cache de máquinas e usuários
        private Dictionary<string, Machine> _machinesCache = new();
        private Dictionary<string, User> _usersCache = new();
        
        // Erro e carregamento
        private bool _hasError;

        public ObservableCollection<MaintenanceRecord> Records
        {
            get => _records;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] Records setter called - new count: {value?.Count ?? 0}");
                SetProperty(ref _records, value);
                _recordsView = CollectionViewSource.GetDefaultView(_records);
                if (_recordsView != null) 
                {
                    _recordsView.Filter = FilterRecords;
                    System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] RecordsView filter set, refreshing view");
                    _recordsView.Refresh();
                }
                OnPropertyChanged(nameof(RecordsView));
            }
        }
        public ICollectionView RecordsView => _recordsView;

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) _currentPage = 1; RefreshAsync(); }
        }

        public string TypeFilter
        {
            get => _typeFilter;
            set { if (SetProperty(ref _typeFilter, value)) _currentPage = 1; RefreshAsync(); }
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set { if (SetProperty(ref _startDate, value)) _currentPage = 1; RefreshAsync(); }
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set { if (SetProperty(ref _endDate, value)) _currentPage = 1; RefreshAsync(); }
        }

        public ObservableCollection<string> AvailableTypes { get; } = new() { "Todos", "Preventiva", "Corretiva" };
        
        public int CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }
        public int TotalPages => (int)Math.Ceiling((double)_totalRecords / PageSize);
        public int TotalRecords { get => _totalRecords; set => SetProperty(ref _totalRecords, value); }
        
        public bool HasError { get => _hasError; set => SetProperty(ref _hasError, value); }
        
        public ICommand LoadRecordsCommand { get; }
        public ICommand ClearDatesCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        private MaintenanceRecord? _selectedRecord;
        public MaintenanceRecord? SelectedRecord
        {
            get => _selectedRecord;
            set { SetProperty(ref _selectedRecord, value); OnPropertyChanged(nameof(IsDetailsOpen)); }
        }
        public bool IsDetailsOpen => SelectedRecord != null;

        private bool _isAdmin;
        public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }

        public ICommand ViewDetailsCommand { get; }
        public ICommand CloseDetailsCommand { get; }

        public HistoryViewModel(ILocalDatabaseService databaseService, IAuthenticationService authService)
        {
            _databaseService = databaseService;
            IsAdmin = authService.GetCurrentUserRole() == "admin";

            System.Diagnostics.Debug.WriteLine("[HistoryViewModel] Constructor called");
            System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] IsAdmin: {IsAdmin}");
            Serilog.Log.Information("[HistoryViewModel] Constructor called - IsAdmin: {IsAdmin}", IsAdmin);

            LoadRecordsCommand = new RelayCommand(async (_) => await LoadRecordsAsync());
            ClearDatesCommand = new RelayCommand(_ => { StartDate = null; EndDate = null; });
            ViewDetailsCommand = new RelayCommand(r => SelectedRecord = r as MaintenanceRecord);
            CloseDetailsCommand = new RelayCommand(_ => SelectedRecord = null);
            DeleteCommand = new RelayCommand(async r => await DeleteRecordAsync(r as MaintenanceRecord));
            NextPageCommand = new RelayCommand(_ => 
            {
                if (CurrentPage < TotalPages) 
                { 
                    CurrentPage++; 
                    _ = LoadPageAsync(); 
                }
            });
            PreviousPageCommand = new RelayCommand(_ => 
            {
                if (CurrentPage > 1) 
                { 
                    CurrentPage--; 
                    _ = LoadPageAsync(); 
                }
            });

            // Initialize collection and view once so bindings and filtering work consistently.
            Records = new ObservableCollection<MaintenanceRecord>();
            _recordsView = CollectionViewSource.GetDefaultView(Records);
            if (_recordsView != null)
            {
                _recordsView.Filter = FilterRecords;
            }
            
            System.Diagnostics.Debug.WriteLine("[HistoryViewModel] Calling LoadRecordsAsync from constructor");
            Serilog.Log.Information("[HistoryViewModel] Calling LoadRecordsAsync from constructor");
            _ = LoadRecordsAsync();
        }

        private async Task DeleteRecordAsync(MaintenanceRecord? record)
        {
            if (record == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Tem certeza que deseja excluir a OS de {record.Type} da máquina {record.MachineTag}?", 
                "Excluir OS", 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var success = await _databaseService.DeleteAsync<MaintenanceRecord>("maintenance_records", record.Id);
                if (success)
                {
                    Records.Remove(record);
                    System.Windows.MessageBox.Show("OS excluída com sucesso!", "Sucesso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Erro ao excluir OS.", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private async Task LoadRecordsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;
            _currentPage = 1;
            
            Serilog.Log.Information("[HistoryViewModel] LoadRecordsAsync started");
            
            try
            {
                System.Diagnostics.Debug.WriteLine("[HistoryViewModel] Iniciando LoadRecordsAsync...");
                Serilog.Log.Information("[HistoryViewModel] Iniciando LoadRecordsAsync...");
                
                // Carregar dados em paralelo (evita I/O sequencial)
                var recordsTask = _databaseService.GetAllAsync<MaintenanceRecord>("maintenance_records");
                var machinesTask = _databaseService.GetAllAsync<Machine>("machines");
                var usersTask = _databaseService.GetAllAsync<User>("profiles");

                await Task.WhenAll(recordsTask, machinesTask, usersTask);

                var allRecords = recordsTask.Result ?? new List<MaintenanceRecord>();
                var machines = machinesTask.Result ?? new List<Machine>();
                var users = usersTask.Result ?? new List<User>();

                System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] Carregados: {allRecords.Count} records, {machines.Count} machines, {users.Count} users");
                Serilog.Log.Information("[HistoryViewModel] Carregados: {RecordsCount} records, {MachinesCount} machines, {UsersCount} users", allRecords.Count, machines.Count, users.Count);

                if (allRecords.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[HistoryViewModel] Sem registros para exibir");
                    Serilog.Log.Information("[HistoryViewModel] Sem registros para exibir");
                    Records.Clear();
                    _allRecords = new();
                    TotalRecords = 0;
                    _recordsView = CollectionViewSource.GetDefaultView(Records);
                    OnPropertyChanged(nameof(RecordsView));
                    return;
                }

                // Construir cache para O(1) lookups
                _machinesCache = machines
                    .Where(m => !string.IsNullOrEmpty(m.Id))
                    .GroupBy(m => m.Id).ToDictionary(g => g.Key!, g => g.First());
                    
                _usersCache = users
                    .Where(u => !string.IsNullOrEmpty(u.Id))
                    .GroupBy(u => u.Id).ToDictionary(g => g.Key!, g => g.First());

                Serilog.Log.Information("[HistoryViewModel] Caches built: {MachinesCacheCount} machines, {UsersCacheCount} users", _machinesCache.Count, _usersCache.Count);

                // Enriquecer dados com cache (em vez de loops aninhados)
                foreach (var record in allRecords)
                {
                    if (record.MachineId != null && _machinesCache.TryGetValue(record.MachineId, out var machine))
                    {
                        record.MachineTag = machine.Tag;
                        record.Area = machine.Area;
                    }
                    if (record.TechnicianId != null && _usersCache.TryGetValue(record.TechnicianId, out var user))
                    {
                        record.TechnicianName = user.Name;
                    }
                }

                _allRecords = allRecords.OrderByDescending(x => x.CreatedAt).ToList();
                TotalRecords = _allRecords.Count;

                Serilog.Log.Information("[HistoryViewModel] About to call LoadPageAsync with {RecordsCount} records", _allRecords.Count);
                await LoadPageAsync();

                _recordsView = CollectionViewSource.GetDefaultView(Records);
                if (_recordsView != null)
                {
                    _recordsView.Filter = FilterRecords;
                }
                OnPropertyChanged(nameof(RecordsView));
                
                Serilog.Log.Information("[HistoryViewModel] LoadRecordsAsync completed successfully - Records.Count={RecordsCount}", Records.Count);
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Erro ao carregar histórico: {ex.Message}";
                Records.Clear();
                _allRecords = new();
                _recordsView = CollectionViewSource.GetDefaultView(Records);
                OnPropertyChanged(nameof(RecordsView));
                System.Diagnostics.Debug.WriteLine($"[HistoryViewModel Error] {ex}");
                Serilog.Log.Error(ex, "[HistoryViewModel] Error in LoadRecordsAsync: {ErrorMessage}", ex.Message);
            }
            finally { IsLoading = false; }
        }

        private async Task LoadPageAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] LoadPageAsync - _allRecords.Count={_allRecords.Count}, _currentPage={_currentPage}");
            Serilog.Log.Information("[HistoryViewModel] LoadPageAsync - _allRecords.Count={AllRecordsCount}, _currentPage={CurrentPage}", _allRecords.Count, _currentPage);
            
            // Aplicar filtros primeiro
            var filtered = _allRecords
                .Where(r => ApplyFilters(r))
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] Após filtros: {filtered.Count} registros");
            Serilog.Log.Information("[HistoryViewModel] Após filtros: {FilteredCount} registros", filtered.Count);

            TotalRecords = filtered.Count;

            // Depois paginar
            var pageRecords = filtered
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] Página {_currentPage}: adicionando {pageRecords.Count} registros ao Records");
            Serilog.Log.Information("[HistoryViewModel] Página {CurrentPage}: adicionando {PageRecordsCount} registros ao Records", _currentPage, pageRecords.Count);

            Records.Clear();
            foreach (var r in pageRecords)
            {
                Records.Add(r);
            }

            _recordsView?.Refresh();

            System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] Após adicionar: Records.Count={Records.Count}, RecordsView.Count={_recordsView?.Cast<object>().Count() ?? 0}");
            Serilog.Log.Information("[HistoryViewModel] Após adicionar: Records.Count={RecordsCount}, RecordsView.Count={RecordsViewCount}", Records.Count, _recordsView?.Cast<object>().Count() ?? 0);

            OnPropertyChanged(nameof(TotalPages));
        }

        private async void RefreshAsync()
        {
            _currentPage = 1;
            await LoadPageAsync();
        }

        private bool FilterRecords(object obj)
        {
            if (obj is not MaintenanceRecord r) return false;
            return ApplyFilters(r);
        }

        private bool ApplyFilters(MaintenanceRecord r)
        {
            // Type Filter
            if (TypeFilter != "Todos")
            {
                if (!r.Type.ToString().Equals(TypeFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Date Filter
            if (StartDate.HasValue || EndDate.HasValue)
            {
                var recDate = r.CreatedAt.Date;
                if (StartDate.HasValue && recDate < StartDate.Value.Date) return false;
                if (EndDate.HasValue && recDate > EndDate.Value.Date) return false;
            }

            // Search Filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                bool matches = (r.MachineTag?.ToLower().Contains(search) == true) ||
                               (r.TechnicianName?.ToLower().Contains(search) == true) ||
                               (r.Description?.ToLower().Contains(search) == true) ||
                               (r.RootCause?.ToLower().Contains(search) == true);
                if (!matches) return false;
            }

            return true;
        }
    }
}
