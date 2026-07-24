using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TTXEquipamentos.Models;
using TTXEquipamentos.Services;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class CalendarViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;

        private ObservableCollection<MaintenancePlan> _scheduledItems = new();
        private DateTime _currentMonth = DateTime.Today;
        private int _selectedYear;
        private int _selectedMonth;

        public ObservableCollection<MaintenancePlan> ScheduledItems
        {
            get => _scheduledItems;
            set => SetProperty(ref _scheduledItems, value);
        }

        public int SelectedYear { get => _selectedYear; set { if (SetProperty(ref _selectedYear, value)) _ = LoadDataAsync(); } }
        public int SelectedMonth { get => _selectedMonth; set { if (SetProperty(ref _selectedMonth, value)) _ = LoadDataAsync(); } }
        public string CurrentMonthLabel => new DateTime(SelectedYear, SelectedMonth, 1).ToString("MMMM yyyy");

        public ICommand PrevMonthCommand { get; }
        public ICommand NextMonthCommand { get; }

        public CalendarViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            SelectedYear = _currentMonth.Year;
            SelectedMonth = _currentMonth.Month;

            PrevMonthCommand = new RelayCommand((_) => {
                _currentMonth = _currentMonth.AddMonths(-1);
                SelectedYear = _currentMonth.Year;
                SelectedMonth = _currentMonth.Month;
                OnPropertyChanged(nameof(CurrentMonthLabel));
            });
            NextMonthCommand = new RelayCommand((_) => {
                _currentMonth = _currentMonth.AddMonths(1);
                SelectedYear = _currentMonth.Year;
                SelectedMonth = _currentMonth.Month;
                OnPropertyChanged(nameof(CurrentMonthLabel));
            });

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                var allPlans = await _databaseService.GetAllAsync<MaintenancePlan>("maintenance_plan");
                var filtered = allPlans
                    .Where(p => p.Year == SelectedYear && 
                           p.ScheduledDate.Month == SelectedMonth)
                    .OrderBy(p => p.ScheduledDate)
                    .ToList();

                ScheduledItems.Clear();
                foreach (var item in filtered) ScheduledItems.Add(item);
                OnPropertyChanged(nameof(CurrentMonthLabel));
            });
        }
    }
}
