using System;
using System.Text.Json.Serialization;

namespace TTXEquipamentos.Models
{
    // Enums
    public enum AppRole
    {
        admin,
        supervisor,
        tecnico
    }

    public enum MaintenanceType
    {
        preventiva,
        instalacao,
        corretiva
    }

    public enum MaintenanceNature
    {
        eletrica,
        mecanica,
        outro
    }

    public enum ChecklistStatus
    {
        ok,
        nao_ok,
        na
    }

    // Models
    public class User
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UserRole
    {
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; }
    }

    public class Profile
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Collaborator
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Function { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Machine
    {
        public string? Id { get; set; }
        public string? Tag { get; set; }
        public string? Name { get; set; }
        public string? Area { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ChecklistItem
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public ChecklistStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class ChecklistTemplate
    {
        public string? Id { get; set; }
        public string? MachineId { get; set; }
        public string? MachineTag { get; set; }
        public List<ChecklistItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MaintenanceItem
    {
        public string? Id { get; set; }
        public string? MaintenanceRecordId { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public double UnitCost { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MaintenanceReportItem
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MaintenanceReportPhoto
    {
        public string? Id { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class MaintenanceRecord
    {
        public string? Id { get; set; }
        public int ReportNumber { get; set; }
        public MaintenanceType Type { get; set; }
        public MaintenanceNature Nature { get; set; }
        public string? CustomNature { get; set; }
        public string? MachineId { get; set; }
        public string? MachineTag { get; set; }
        public string? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public string? RequesterId { get; set; }
        public string? RequesterName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? WeatherMorning { get; set; }
        public string? WeatherAfternoon { get; set; }
        public string? WeatherNight { get; set; }
        public string? RainfallIndex { get; set; }
        public string? Description { get; set; }
        public string? RootCause { get; set; }
        public string? WorkDescription { get; set; }
        [JsonPropertyName("observacoes")]
        public string? Observations { get; set; }
        public string? Area { get; set; }
        public string? Requester { get; set; }
        public string? Status { get; set; }
        public List<ChecklistItem> ChecklistItems { get; set; } = new();
        [JsonPropertyName("itens_utilizados")]
        public List<MaintenanceItem> Items { get; set; } = new();
        public List<MaintenanceReportItem> LaborForce { get; set; } = new();
        public List<MaintenanceReportItem> EquipmentItems { get; set; } = new();
        [JsonPropertyName("falha_aparente")]
        public List<MaintenanceReportItem> Activities { get; set; } = new();
        [JsonPropertyName("trabalho_executado")]
        public List<MaintenanceReportItem> Incidents { get; set; } = new();
        [JsonPropertyName("comentarios")]
        public List<MaintenanceReportItem> Comments { get; set; } = new();
        public List<MaintenanceReportPhoto> Photos { get; set; } = new();
        public double TotalCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MaintenancePlan
    {
        public string? Id { get; set; }
        public string? MachineTag { get; set; }
        public int WeekNumber { get; set; }
        public MaintenanceType MaintenanceType { get; set; }
        public int Year { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Requester
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Department { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Contractor
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Specialty { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public double HourlyRate { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ExtraCost
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public double Amount { get; set; }
        public string? ContractorId { get; set; }
        public string? ContractorName { get; set; }
        public DateTime ServiceDate { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SystemDiagnostics
    {
        public string? Id { get; set; }
        public string? ServerIp { get; set; }
        public bool IsOnline { get; set; }
        public string? LastCommand { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AlertSettings
    {
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public bool AlertsEnabled { get; set; }
        public string? EmailRecipient { get; set; }
        public string? AlertFrequency { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class EmailSettings
    {
        public string? Id { get; set; }
        public string? SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string? FromEmail { get; set; }
        public string? Password { get; set; }
        public bool UseSSL { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
