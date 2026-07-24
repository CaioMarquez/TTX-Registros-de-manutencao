using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using TTXEquipamentos.Models;
using TTXEquipamentos.Services;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class IndicatorsViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;

        // Backing fields
        private double _availability;
        private double _currentMonthCost;
        private double _avgMonthlyCost;
        private double _currentMonthDowntime;
        private int _totalMachines;
        private int _totalOs;
        private int _preventiveOs;
        private int _correctiveOs;
        private string _topMachineName = "—";
        private double _topMachineCost;
        private double _avgDowntimePerCorrective;
        private double _yearlyCost;

        public IndicatorsViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            _ = LoadDataAsync();
        }

        public double Availability { get => _availability; set => SetProperty(ref _availability, value); }
        public double CurrentMonthCost { get => _currentMonthCost; set => SetProperty(ref _currentMonthCost, value); }
        public double AvgMonthlyCost { get => _avgMonthlyCost; set => SetProperty(ref _avgMonthlyCost, value); }
        public double CurrentMonthDowntime { get => _currentMonthDowntime; set => SetProperty(ref _currentMonthDowntime, value); }
        public int TotalMachines { get => _totalMachines; set => SetProperty(ref _totalMachines, value); }
        public int TotalOs { get => _totalOs; set => SetProperty(ref _totalOs, value); }
        public int PreventiveOs { get => _preventiveOs; set => SetProperty(ref _preventiveOs, value); }
        public int CorrectiveOs { get => _correctiveOs; set => SetProperty(ref _correctiveOs, value); }
        public string TopMachineName { get => _topMachineName; set => SetProperty(ref _topMachineName, value); }
        public double TopMachineCost { get => _topMachineCost; set => SetProperty(ref _topMachineCost, value); }
        public double AvgDowntimePerCorrective { get => _avgDowntimePerCorrective; set => SetProperty(ref _avgDowntimePerCorrective, value); }
        public double YearlyCost { get => _yearlyCost; set => SetProperty(ref _yearlyCost, value); }

        public string AvailabilityFormatted => $"{Availability:F1}%";
        public string CurrentMonthCostFormatted => CurrentMonthCost.ToString("C0", new System.Globalization.CultureInfo("pt-BR"));
        public string AvgMonthlyCostFormatted => AvgMonthlyCost.ToString("C0", new System.Globalization.CultureInfo("pt-BR"));
        public string CurrentMonthDowntimeFormatted => $"{Math.Round(CurrentMonthDowntime)}h";
        public string TotalOsFormatted => $"{TotalOs} OS";
        public string CostInsight => string.IsNullOrEmpty(TopMachineName) ? "" : $"{TopMachineName} concentra {TopMachineCost.ToString("C0", new System.Globalization.CultureInfo("pt-BR"))} em custos";
        public string OsMixInsight => $"{PreventiveOs} preventivas e {CorrectiveOs} corretivas";
        public string DowntimeInsight => $"{Math.Round(AvgDowntimePerCorrective, 1)}h média por corretiva";

        public PlotModel MonthlyCostsModel { get; private set; } = new PlotModel();
        public PlotModel AnnualCorrectiveModel { get; private set; } = new PlotModel();
        public PlotModel MachineCostsModel { get; private set; } = new PlotModel();

        public ICommand LoadDataCommand { get; }

        private async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var records = await _databaseService.GetAllAsync<MaintenanceRecord>("maintenance_records");
                var machines = await _databaseService.GetAllAsync<Machine>("machines");
                var extraCosts = await _databaseService.GetAllAsync<ExtraCost>("extra_costs") ?? new List<ExtraCost>();

                TotalMachines = machines?.Count ?? 0;
                TotalOs = records?.Count ?? 0;
                PreventiveOs = records.Count(r => r.Type == MaintenanceType.preventiva);
                CorrectiveOs = records.Count(r => r.Type == MaintenanceType.corretiva);

                BuildMonthlyCostsModel(records, extraCosts);
                BuildAnnualCorrectiveModel(records);
                BuildMachineCostsModel(records);

                OnPropertyChanged(nameof(AvailabilityFormatted));
                OnPropertyChanged(nameof(CurrentMonthCostFormatted));
                OnPropertyChanged(nameof(AvgMonthlyCostFormatted));
                OnPropertyChanged(nameof(CurrentMonthDowntimeFormatted));
                OnPropertyChanged(nameof(TotalOsFormatted));
                OnPropertyChanged(nameof(CostInsight));
                OnPropertyChanged(nameof(OsMixInsight));
                OnPropertyChanged(nameof(DowntimeInsight));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void BuildMonthlyCostsModel(List<MaintenanceRecord> records, List<ExtraCost> extraCosts)
        {
            var now = DateTime.Now;
            int year = now.Year;

            var model = new PlotModel
            {
                Title = "Custos mensais",
                TitleFontSize = 14,
                Padding = new OxyThickness(60, 12, 60, 50),
                PlotAreaBorderThickness = new OxyThickness(0),
                Background = OxyColors.White
            };
            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Key = "CategoryAxis",
                TextColor = OxyColor.Parse("#374151"),
                Angle = 0,
                GapWidth = 0.4,
                IsTickCentered = true,
                AxislineStyle = LineStyle.Solid,
                MajorTickSize = 0,
                MinorTickSize = 0
            };
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Key = "ValueAxis",
                LabelFormatter = v => v >= 1000 ? $"R$ {v / 1000:0} mil" : $"R$ {v:0}",
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.Parse("#e5e7eb"),
                MinimumPadding = 0,
                AbsoluteMinimum = 0,
                MaximumPadding = 0.05
            };

            var partsSeries = new BarSeries
            {
                Title = "Peças",
                FillColor = OxyColor.Parse("#16a34a"),
                LabelFormatString = "{0:C0}",
                LabelPlacement = LabelPlacement.Outside,
                LabelMargin = 4,
                BarWidth = 0.55,
                XAxisKey = "ValueAxis",
                YAxisKey = "CategoryAxis",
                IsStacked = true
            };
            var laborSeries = new BarSeries
            {
                Title = "Custos Extras",
                FillColor = OxyColor.Parse("#2563eb"),
                LabelFormatString = "{0:C0}",
                LabelPlacement = LabelPlacement.Outside,
                LabelMargin = 4,
                BarWidth = 0.55,
                XAxisKey = "ValueAxis",
                YAxisKey = "CategoryAxis",
                IsStacked = true
            };
            var avgSeries = new LineSeries
            {
                Title = "Média Anual",
                Color = OxyColor.Parse("#f97316"),
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerFill = OxyColor.Parse("#f97316")
            };

            var monthlyTotals = new List<double>();
            double currentMonthCost = 0;
            double currentMonthDowntime = 0;

            for (int m = 1; m <= now.Month; m++)
            {
                var mStart = new DateTime(year, m, 1);
                var mEnd = mStart.AddMonths(1).AddDays(-1);
                if (mEnd > now) mEnd = now;

                var mRecs = records.Where(r => r.StartTime >= mStart && r.StartTime <= mEnd).ToList();
                double parts = mRecs.Sum(r => r.TotalCost);
                double labor = extraCosts.Where(e => e.InvoiceDate.Month == m && e.InvoiceDate.Year == year).Sum(e => e.Amount);
                double downtime = mRecs.Where(r => r.Type == MaintenanceType.corretiva && r.EndTime.HasValue).Sum(r => CalculateDowntime(r.StartTime, r.EndTime.Value));

                monthlyTotals.Add(parts + labor);
                categoryAxis.Labels.Add(mStart.ToString("MMM", new System.Globalization.CultureInfo("pt-BR")).ToUpper());
                partsSeries.Items.Add(new BarItem(parts));
                laborSeries.Items.Add(new BarItem(labor));

                if (m == now.Month)
                {
                    currentMonthCost = parts + labor;
                    currentMonthDowntime = downtime;
                }
            }

            double avg = monthlyTotals.Any() ? monthlyTotals.Average() : 0;
            for (int i = 0; i < monthlyTotals.Count; i++) avgSeries.Points.Add(new DataPoint(i, avg));

            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);
            model.Series.Add(partsSeries);
            model.Series.Add(laborSeries);
            model.Series.Add(avgSeries);
            model.Legends.Add(new OxyPlot.Legends.Legend
            {
                LegendPosition = OxyPlot.Legends.LegendPosition.BottomCenter,
                LegendPlacement = OxyPlot.Legends.LegendPlacement.Outside,
                LegendOrientation = OxyPlot.Legends.LegendOrientation.Horizontal,
                LegendItemAlignment = OxyPlot.HorizontalAlignment.Left,
                LegendItemOrder = OxyPlot.Legends.LegendItemOrder.Normal
            });

            MonthlyCostsModel = model;
            CurrentMonthCost = currentMonthCost;
            AvgMonthlyCost = avg;
            CurrentMonthDowntime = currentMonthDowntime;
        }

        private void BuildAnnualCorrectiveModel(List<MaintenanceRecord> records)
        {
            int year = DateTime.Now.Year;
            var model = new PlotModel { Title = "Corretivas por mês", TitleFontSize = 14, Padding = new OxyThickness(60, 12, 60, 40), PlotAreaBorderThickness = new OxyThickness(0) };
            var corrCategoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Key = "CategoryAxis", TextColor = OxyColor.Parse("#6B7280"), Angle = 0, GapWidth = 0.4, MajorGridlineStyle = LineStyle.None, MinorGridlineStyle = LineStyle.None };
            var hoursAxis = new LinearAxis { Position = AxisPosition.Left, Key = "ValueAxis", Title = "Horas Paradas", MajorGridlineStyle = LineStyle.Solid, MajorGridlineColor = OxyColor.Parse("#EEF2FF"), MinimumPadding = 0, AbsoluteMinimum = 0, MaximumPadding = 0.08 };
            var osAxis = new LinearAxis { Position = AxisPosition.Right, Title = "Qtd. O.S.", Key = "OSAxis", MinimumPadding = 0, AbsoluteMinimum = 0, MaximumPadding = 0.08 };

            model.Axes.Add(corrCategoryAxis);
            model.Axes.Add(hoursAxis);
            model.Axes.Add(osAxis);

            var correctiveRecords = records.Where(r => r.Type == MaintenanceType.corretiva && r.StartTime.Year == year && r.EndTime.HasValue).ToList();

            var hoursSeries = new BarSeries { Title = "Horas", FillColor = OxyColor.Parse("#06b6d4"), LabelFormatString = "{0:F0}", LabelPlacement = LabelPlacement.Outside, LabelMargin = 4, BarWidth = 0.45, XAxisKey = "ValueAxis", YAxisKey = "CategoryAxis" };
            var osLineSeries = new LineSeries { Title = "O.S. Corretivas", Color = OxyColor.Parse("#ef4444"), StrokeThickness = 2, MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = OxyColor.Parse("#ef4444"), YAxisKey = "OSAxis" };

            for (int m = 1; m <= 12; m++)
            {
                var mStart = new DateTime(year, m, 1);
                corrCategoryAxis.Labels.Add(mStart.ToString("MMM", new System.Globalization.CultureInfo("pt-BR")).ToUpper());

                var mRecs = correctiveRecords.Where(r => r.StartTime.Month == m).ToList();
                double hours = mRecs.Sum(r => CalculateDowntime(r.StartTime, r.EndTime.Value));
                hoursSeries.Items.Add(new BarItem(hours));

                osLineSeries.Points.Add(new DataPoint(m - 1, mRecs.Count));
            }

            model.Series.Add(hoursSeries);
            model.Series.Add(osLineSeries);
            model.Legends.Add(new OxyPlot.Legends.Legend { LegendPosition = OxyPlot.Legends.LegendPosition.TopRight, LegendPlacement = OxyPlot.Legends.LegendPlacement.Outside, LegendOrientation = OxyPlot.Legends.LegendOrientation.Horizontal, LegendItemAlignment = OxyPlot.HorizontalAlignment.Right });
            AnnualCorrectiveModel = model;

            var corr = correctiveRecords;
            double totalDowntime = corr.Sum(r => CalculateDowntime(r.StartTime, r.EndTime.Value));
            AvgDowntimePerCorrective = corr.Count > 0 ? totalDowntime / corr.Count : 0;
        }

        private void BuildMachineCostsModel(List<MaintenanceRecord> records)
        {
            var model = new PlotModel { Title = "Gastos por máquina", TitleFontSize = 14, Padding = new OxyThickness(100, 12, 100, 40) };
            var mcCategoryAxis = new CategoryAxis { Position = AxisPosition.Left, TextColor = OxyColor.Parse("#374151"), GapWidth = 0.4 };
            var mcValueAxis = new LinearAxis { Position = AxisPosition.Bottom, StringFormat = "C0", MajorGridlineStyle = LineStyle.Solid, MajorGridlineColor = OxyColor.Parse("#e5e7eb"), MinimumPadding = 0, AbsoluteMinimum = 0 };

            var mcSeries = new BarSeries { FillColor = OxyColor.Parse("#2563eb"), LabelFormatString = "{0:C0}", LabelPlacement = LabelPlacement.Inside, LabelMargin = 6 };
            var machineCostList = records.GroupBy(r => r.MachineTag ?? "Outras").Select(g => new { Name = g.Key, Cost = g.Sum(r => r.TotalCost) }).Where(x => x.Cost > 0).OrderBy(x => x.Cost).ToList();

            foreach (var mc in machineCostList)
            {
                mcCategoryAxis.Labels.Add(mc.Name.Length > 25 ? mc.Name.Substring(0, 22) + "..." : mc.Name);
                mcSeries.Items.Add(new BarItem(mc.Cost));
            }

            model.Axes.Add(mcCategoryAxis);
            model.Axes.Add(mcValueAxis);
            model.Series.Add(mcSeries);
            MachineCostsModel = model;

            if (machineCostList.Any())
            {
                TopMachineName = machineCostList.OrderByDescending(x => x.Cost).First().Name;
                TopMachineCost = machineCostList.OrderByDescending(x => x.Cost).First().Cost;
            }
            else
            {
                TopMachineName = "Sem máquina";
                TopMachineCost = 0;
            }

            YearlyCost = records.Sum(r => r.TotalCost);
        }

        private double CalculateDowntime(DateTime start, DateTime end)
        {
            if (start >= end) return 0;
            double hours = 0;
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;

                var wStart = date.AddHours(7).AddMinutes(30);
                var wEnd = date.AddHours(date.DayOfWeek == DayOfWeek.Friday ? 16 : 17).AddMinutes(30);

                var overlapStart = start > wStart ? start : wStart;
                var overlapEnd = end < wEnd ? end : wEnd;

                if (overlapStart < overlapEnd)
                    hours += (overlapEnd - overlapStart).TotalHours;
            }
            return hours;
        }
    }
}
