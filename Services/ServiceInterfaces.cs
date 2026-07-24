namespace TTXEquipamentos.Services
{
    public interface IAuthenticationService
    {
        Task<bool> LoginAsync(string email, string password);
        Task<(bool Success, string? Error)> SignupAsync(string name, string email, string password);
        Task<bool> LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        string? GetCurrentUserRole();
        string? GetCurrentUserId();
        string? GetCurrentUserName();
        string? GetCurrentUserEmail();
        Task<(bool Success, string? Error)> UpdatePasswordAsync(string newPassword);
    }

    public interface IMaintenanceCalculationsService
    {
        double CalculateMonthlyScheduledHours(int year, int month);
        double CalculateDowntimeInWorkHours(int year, int month);
        double CalculateAvailability(int year, int month);
        Task<Dictionary<string, object>> GetMonthlyMetricsAsync(int year, int month);
    }

    public interface IServerDiagnosticsService
    {
        Task<bool> SendHeartbeatAsync();
        Task<string> GetServerStatusAsync();
        Task LogCommandAsync(string command);
    }

    public interface INavigationService
    {
        void NavigateToPage(string pageName, object? parameter = null);
        void GoBack();
        void NavigateToAuth();
        void NavigateToDashboard();
    }

    public interface IAlertConfigurationService
    {
        Task<Dictionary<string, object>> GetAlertSettingsAsync(string userId);
        Task<bool> SaveAlertSettingsAsync(string userId, Dictionary<string, object> settings);
    }

    public interface ILocalDatabaseService
    {
        void Initialize();
        
        // Generic CRUD
        Task<List<T>> GetAllAsync<T>(string entityType) where T : class;
        Task<T?> GetByIdAsync<T>(string entityType, string id) where T : class;
        Task<bool> SaveAsync<T>(string entityType, T entity) where T : class;
        Task<bool> DeleteAsync<T>(string entityType, string id) where T : class;
        
        // Specific entity methods
        Task<Dictionary<string, object>?> GetUserByEmailAsync(string email);
        Task<string?> GetUserRoleAsync(string userId);
        Task<List<Dictionary<string, object>>> GetAllMachinesAsync();
        Task<List<Dictionary<string, object>>> GetAllMaintenanceRecordsAsync();
    }
}
