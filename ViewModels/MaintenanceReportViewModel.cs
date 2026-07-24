using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TTXEquipamentos.Models;

namespace TTXEquipamentos.ViewModels
{
    public class MaintenanceReportViewModel : ViewModelBase
    {
        private int _reportNumber;
        private DateTime _selectedDate;
        private string? _equipment;
        private string? _area;
        private string? _weatherMorning;
        private string? _weatherAfternoon;
        private string? _weatherNight;
        private string? _rainfallIndex;
        private string? _maintenanceDescription;
        private string? _observations;
        private string? _newLaborItem;
        private string? _newEquipmentItem;
        private string? _newActivityItem;
        private string? _newIncidentItem;
        private string? _newCommentItem;

        public ObservableCollection<MaintenanceReportItem> LaborForce { get; }
        public ObservableCollection<MaintenanceReportItem> Equipment { get; }
        public ObservableCollection<MaintenanceReportItem> Activities { get; }
        public ObservableCollection<MaintenanceReportItem> Incidents { get; }
        public ObservableCollection<MaintenanceReportItem> Comments { get; }
        public ObservableCollection<MaintenanceReportPhoto> Photos { get; }

        public ICommand AddLaborCommand { get; }
        public ICommand AddEquipmentCommand { get; }
        public ICommand AddActivityCommand { get; }
        public ICommand AddIncidentCommand { get; }
        public ICommand AddCommentCommand { get; }
        public ICommand AddPhotoCommand { get; }
        public ICommand RemoveLaborCommand { get; }
        public ICommand RemoveEquipmentCommand { get; }
        public ICommand RemoveActivityCommand { get; }
        public ICommand RemoveIncidentCommand { get; }
        public ICommand RemoveCommentCommand { get; }
        public ICommand RemovePhotoCommand { get; }
        public ICommand SaveReportCommand { get; }

        public MaintenanceReportViewModel()
        {
            _selectedDate = DateTime.Now;
            _reportNumber = GenerateReportNumber();

            LaborForce = new ObservableCollection<MaintenanceReportItem>();
            Equipment = new ObservableCollection<MaintenanceReportItem>();
            Activities = new ObservableCollection<MaintenanceReportItem>();
            Incidents = new ObservableCollection<MaintenanceReportItem>();
            Comments = new ObservableCollection<MaintenanceReportItem>();
            Photos = new ObservableCollection<MaintenanceReportPhoto>();

            AddLaborCommand = new RelayCommand(_ => AddLaborItem());
            AddEquipmentCommand = new RelayCommand(_ => AddEquipmentItem());
            AddActivityCommand = new RelayCommand(_ => AddActivityItem());
            AddIncidentCommand = new RelayCommand(_ => AddIncidentItem());
            AddCommentCommand = new RelayCommand(_ => AddCommentItem());
            AddPhotoCommand = new RelayCommand(_ => AddPhoto());

            RemoveLaborCommand = new RelayCommand(param => RemoveItem(LaborForce, param));
            RemoveEquipmentCommand = new RelayCommand(param => RemoveItem(Equipment, param));
            RemoveActivityCommand = new RelayCommand(param => RemoveItem(Activities, param));
            RemoveIncidentCommand = new RelayCommand(param => RemoveItem(Incidents, param));
            RemoveCommentCommand = new RelayCommand(param => RemoveItem(Comments, param));
            RemovePhotoCommand = new RelayCommand(param => RemovePhoto(param));

            SaveReportCommand = new RelayCommand(_ => SaveReport());
        }

        public int ReportNumber
        {
            get => _reportNumber;
            set => SetProperty(ref _reportNumber, value);
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        public string? EquipmentName
        {
            get => _equipment;
            set => SetProperty(ref _equipment, value);
        }

        public string? Area
        {
            get => _area;
            set => SetProperty(ref _area, value);
        }

        public string? WeatherMorning
        {
            get => _weatherMorning;
            set => SetProperty(ref _weatherMorning, value);
        }

        public string? WeatherAfternoon
        {
            get => _weatherAfternoon;
            set => SetProperty(ref _weatherAfternoon, value);
        }

        public string? WeatherNight
        {
            get => _weatherNight;
            set => SetProperty(ref _weatherNight, value);
        }

        public string? RainfallIndex
        {
            get => _rainfallIndex;
            set => SetProperty(ref _rainfallIndex, value);
        }

        public string? MaintenanceDescription
        {
            get => _maintenanceDescription;
            set => SetProperty(ref _maintenanceDescription, value);
        }

        public string? Observations
        {
            get => _observations;
            set => SetProperty(ref _observations, value);
        }

        public string? NewLaborItem
        {
            get => _newLaborItem;
            set => SetProperty(ref _newLaborItem, value);
        }

        public string? NewEquipmentItem
        {
            get => _newEquipmentItem;
            set => SetProperty(ref _newEquipmentItem, value);
        }

        public string? NewActivityItem
        {
            get => _newActivityItem;
            set => SetProperty(ref _newActivityItem, value);
        }

        public string? NewIncidentItem
        {
            get => _newIncidentItem;
            set => SetProperty(ref _newIncidentItem, value);
        }

        public string? NewCommentItem
        {
            get => _newCommentItem;
            set => SetProperty(ref _newCommentItem, value);
        }

        public string DayOfWeek => SelectedDate.ToString("dddd", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

        private int GenerateReportNumber()
        {
            // Generate a number based on date and random component
            return int.Parse(DateTime.Now.ToString("yyyyMMdd")) % 100 + new Random().Next(1, 100);
        }

        private void AddLaborItem()
        {
            if (!string.IsNullOrWhiteSpace(NewLaborItem))
            {
                LaborForce.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewLaborItem, CreatedAt = DateTime.Now });
                NewLaborItem = string.Empty;
            }
        }

        private void AddEquipmentItem()
        {
            if (!string.IsNullOrWhiteSpace(NewEquipmentItem))
            {
                Equipment.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewEquipmentItem, CreatedAt = DateTime.Now });
                NewEquipmentItem = string.Empty;
            }
        }

        private void AddActivityItem()
        {
            if (!string.IsNullOrWhiteSpace(NewActivityItem))
            {
                Activities.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewActivityItem, CreatedAt = DateTime.Now });
                NewActivityItem = string.Empty;
            }
        }

        private void AddIncidentItem()
        {
            if (!string.IsNullOrWhiteSpace(NewIncidentItem))
            {
                Incidents.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewIncidentItem, CreatedAt = DateTime.Now });
                NewIncidentItem = string.Empty;
            }
        }

        private void AddCommentItem()
        {
            if (!string.IsNullOrWhiteSpace(NewCommentItem))
            {
                Comments.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewCommentItem, CreatedAt = DateTime.Now });
                NewCommentItem = string.Empty;
            }
        }

        private void AddPhoto()
        {
            // This will be handled by the code-behind with file dialog
            var item = new MaintenanceReportPhoto { Id = Guid.NewGuid().ToString(), UploadedAt = DateTime.Now };
            Photos.Add(item);
        }

        private void RemoveItem(ObservableCollection<MaintenanceReportItem> collection, object? param)
        {
            if (param is MaintenanceReportItem item)
            {
                collection.Remove(item);
            }
        }

        private void RemovePhoto(object? param)
        {
            if (param is MaintenanceReportPhoto photo)
            {
                Photos.Remove(photo);
            }
        }

        private void SaveReport()
        {
            var report = new MaintenanceReport
            {
                Id = Guid.NewGuid().ToString(),
                ReportNumber = ReportNumber,
                Date = SelectedDate,
                EquipmentName = EquipmentName,
                Area = Area,
                WeatherMorning = WeatherMorning,
                WeatherAfternoon = WeatherAfternoon,
                WeatherNight = WeatherNight,
                RainfallIndex = RainfallIndex,
                MaintenanceDescription = MaintenanceDescription,
                Observations = Observations,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            foreach (var item in LaborForce)
                report.LaborForce.Add(item);

            foreach (var item in Equipment)
                report.EquipmentItems.Add(item);

            if (!string.IsNullOrWhiteSpace(NewActivityItem))
                Activities.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewActivityItem, CreatedAt = DateTime.Now });
            foreach (var item in Activities)
                report.Activities.Add(item);

            if (!string.IsNullOrWhiteSpace(NewIncidentItem))
                Incidents.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewIncidentItem, CreatedAt = DateTime.Now });
            foreach (var item in Incidents)
                report.Incidents.Add(item);

            if (!string.IsNullOrWhiteSpace(NewCommentItem))
                Comments.Add(new MaintenanceReportItem { Id = Guid.NewGuid().ToString(), Description = NewCommentItem, CreatedAt = DateTime.Now });
            foreach (var item in Comments)
                report.Comments.Add(item);

            foreach (var photo in Photos)
                report.Photos.Add(photo);

            // TODO: Save to database/file service
            // For now, just show success feedback through UI binding
        }
    }
}
