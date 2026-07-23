using System;
using System.Collections.Generic;

namespace TTXEquipamentos.Models
{
    public class MaintenanceReport
    {
        public string? Id { get; set; }
        public int ReportNumber { get; set; }
        public DateTime Date { get; set; }
        public string? EquipmentName { get; set; }
        public string? Area { get; set; }
        
        // Weather/Conditions
        public string? WeatherMorning { get; set; }
        public string? WeatherAfternoon { get; set; }
        public string? WeatherNight { get; set; }
        public string? RainfallIndex { get; set; }
        
        // Maintenance Description
        public string? MaintenanceDescription { get; set; }
        public string? Observations { get; set; }

        // Collections
        public List<MaintenanceReportItem> LaborForce { get; set; } = new();
        public List<MaintenanceReportItem> EquipmentItems { get; set; } = new();
        public List<MaintenanceReportItem> Activities { get; set; } = new();
        public List<MaintenanceReportItem> Incidents { get; set; } = new();
        public List<MaintenanceReportItem> Comments { get; set; } = new();
        public List<MaintenanceReportPhoto> Photos { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
