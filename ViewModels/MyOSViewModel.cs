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
    public class MyOSViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;
        private readonly IAuthenticationService _authService;
        private List<MaintenanceRecord> _allRecords = new();
        private ObservableCollection<MaintenanceRecord> _records = new();
        private ICollectionView _recordsView;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private bool _hasError;

        // Paginação
        private const int PageSize = 15;
        private int _currentPage = 1;
        private int _totalRecords = 0;

        public ObservableCollection<MaintenanceRecord> Records
        {
            get => _records;
            set
            {
                SetProperty(ref _records, value);
                _recordsView = CollectionViewSource.GetDefaultView(_records);
                if (_recordsView != null) _recordsView.Filter = FilterRecords;
            }
        }
        public ICollectionView RecordsView => _recordsView;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value)) { _currentPage = 1; RefreshAsync(); }
            }
        }

        public int CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }
        public int TotalPages => (int)Math.Ceiling((double)_totalRecords / PageSize);
        public int TotalRecords { get => _totalRecords; set => SetProperty(ref _totalRecords, value); }

        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public ICommand LoadRecordsCommand { get; }

        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public bool HasError { get => _hasError; set => SetProperty(ref _hasError, value); }

        private MaintenanceRecord? _selectedRecord;
        public MaintenanceRecord? SelectedRecord
        {
            get => _selectedRecord;
            set { SetProperty(ref _selectedRecord, value); OnPropertyChanged(nameof(IsDetailsOpen)); }
        }

        public bool IsDetailsOpen => SelectedRecord != null;

        public ICommand ViewDetailsCommand { get; }
        public ICommand CloseDetailsCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        public MyOSViewModel(ILocalDatabaseService databaseService, IAuthenticationService authService)
        {
            _databaseService = databaseService;
            _authService = authService;
            
            LoadRecordsCommand = new RelayCommand(async (_) => await LoadRecordsAsync());
            ViewDetailsCommand = new RelayCommand(r => SelectedRecord = r as MaintenanceRecord);
            CloseDetailsCommand = new RelayCommand(_ => SelectedRecord = null);
            
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
            
            _ = LoadRecordsAsync();
        }

        private async Task LoadRecordsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;
            _currentPage = 1;
            
            try
            {
                var currentUserId = _authService.GetCurrentUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    throw new InvalidOperationException("Usuário não autenticado");
                }

                // Carregamentos em paralelo
                var recordsTask = _databaseService.GetAllAsync<MaintenanceRecord>("maintenance_records");
                var machinesTask = _databaseService.GetAllAsync<Machine>("machines");

                await Task.WhenAll(recordsTask, machinesTask);
                
                var allRecords = recordsTask.Result ?? new();
                var machines = machinesTask.Result ?? new();

                if (allRecords == null || allRecords.Count == 0)
                {
                    Records.Clear();
                    _allRecords = new();
                    TotalRecords = 0;
                    _recordsView = CollectionViewSource.GetDefaultView(Records);
                    OnPropertyChanged(nameof(RecordsView));
                    return;
                }

                // Permit all users to ver todos os registros
                var myRecords = allRecords
                    .ToList();

                if (myRecords.Count == 0)
                {
                    Records.Clear();
                    _allRecords = new();
                    TotalRecords = 0;
                    _recordsView = CollectionViewSource.GetDefaultView(Records);
                    OnPropertyChanged(nameof(RecordsView));
                    return;
                }

                // Build machines cache for O(1) lookup
                var machinesDict = machines
                    .Where(m => !string.IsNullOrEmpty(m.Id))
                    .GroupBy(m => m.Id).ToDictionary(g => g.Key!, g => g.First());

                // Enrich records with machine data
                foreach (var record in myRecords)
                {
                    if (record.MachineId != null && machinesDict.TryGetValue(record.MachineId, out var machine))
                    {
                        record.Area = machine.Area;
                        record.MachineTag = machine.Tag;
                    }
                }

                _allRecords = myRecords.OrderByDescending(x => x.CreatedAt).ToList();
                TotalRecords = _allRecords.Count;

                await LoadPageAsync();

                _recordsView = CollectionViewSource.GetDefaultView(Records);
                if (_recordsView != null)
                {
                    _recordsView.Filter = FilterRecords;
                }
                OnPropertyChanged(nameof(RecordsView));
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Erro ao carregar OS: {ex.Message}";
                Records.Clear();
                _allRecords = new();
                _recordsView = CollectionViewSource.GetDefaultView(Records);
                OnPropertyChanged(nameof(RecordsView));
                System.Diagnostics.Debug.WriteLine($"[MyOSViewModel Error] {ex}");
            }
            finally 
            { 
                IsLoading = false; 
            }
        }

        private async Task LoadPageAsync()
        {
            var filtered = _allRecords
                .Where(r => ApplyFilters(r))
                .ToList();

            TotalRecords = filtered.Count;

            var pageRecords = filtered
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Records.Clear();
            foreach (var r in pageRecords) 
                Records.Add(r);

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
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                return (r.MachineTag?.ToLower().Contains(search) == true) ||
                       (r.Description?.ToLower().Contains(search) == true) ||
                       (r.Status?.ToLower().Contains(search) == true);
            }
            return true;
        }
    }
}
