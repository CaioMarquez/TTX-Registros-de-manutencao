using TTXEquipamentos.Data;
using TTXEquipamentos.Models;
using System.Linq;

namespace TTXEquipamentos.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILocalDatabaseService _databaseService;
        private string? _currentUserId;
        private string? _currentUserRole;
        private string? _currentUserName;
        private string? _currentUserEmail;

        public AuthenticationService(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _databaseService.GetUserByEmailAsync(email);
                if (user == null) return false;

                // Simple password check (in production, use proper hashing)
                if (user.TryGetValue("password", out var pwd) && pwd?.ToString() == password)
                {
                    _currentUserId = user.TryGetValue("id", out var id) ? id?.ToString() : null;
                    _currentUserName = user.TryGetValue("name", out var name) ? name?.ToString() : null;
                    _currentUserEmail = user.TryGetValue("email", out var userEmail) ? userEmail?.ToString() : null;
                    _currentUserRole = await _databaseService.GetUserRoleAsync(_currentUserId ?? string.Empty);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string? Error)> SignupAsync(string name, string email, string password)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _databaseService.GetUserByEmailAsync(email);
                if (existingUser != null)
                    return (false, "Email já está registrado.");

                // Create new user
                var newUser = new Dictionary<string, object>
                {
                    { "id", Guid.NewGuid().ToString() },
                    { "name", name },
                    { "email", email },
                    { "password", password },
                    { "created_at", DateTime.Now },
                    { "updated_at", DateTime.Now }
                };

                // Save to database
                await _databaseService.SaveAsync("profiles", newUser);

                // User is pending approval - no auto-login or role assignment
                return (true, "Cadastro realizado! Aguarde a aprovação do administrador para acessar o sistema.");
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao criar conta: {ex.Message}");
            }
        }

        public Task<bool> LogoutAsync()
        {
            _currentUserId = null;
            _currentUserRole = null;
            _currentUserName = null;
            _currentUserEmail = null;
            return Task.FromResult(true);
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            return Task.FromResult(!string.IsNullOrEmpty(_currentUserId));
        }

        public string? GetCurrentUserRole()
        {
            return _currentUserRole;
        }

        public string? GetCurrentUserId()
        {
            return _currentUserId;
        }

        public string? GetCurrentUserName()
        {
            return _currentUserName;
        }

        public string? GetCurrentUserEmail()
        {
            return _currentUserEmail;
        }

        public async Task<(bool Success, string? Error)> UpdatePasswordAsync(string newPassword)
        {
            if (string.IsNullOrEmpty(_currentUserId))
                return (false, "Usuário não autenticado.");

            try
            {
                var profiles = await _databaseService.GetAllAsync<Profile>("profiles");
                var userProfile = profiles.FirstOrDefault(p => p.Id == _currentUserId);
                if (userProfile == null)
                    return (false, "Perfil não encontrado.");

                // To save it correctly to the JSON mapping, we must convert Profile to Dictionary, or just do a save
                var userDict = await _databaseService.GetUserByEmailAsync(_currentUserEmail!);
                if (userDict != null)
                {
                    userDict["password"] = newPassword;
                    userDict["updated_at"] = DateTime.Now;
                    await _databaseService.SaveAsync("profiles", userDict);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }

    public class MaintenanceCalculationsService : IMaintenanceCalculationsService
    {
        private const int TOTAL_MACHINES = 70;
        private const double AVAILABILITY_TARGET = 0.95;
        private readonly ILocalDatabaseService _databaseService;

        public MaintenanceCalculationsService(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public double CalculateMonthlyScheduledHours(int year, int month)
        {
            // Mon-Thu: 10 hours/day (07:30-17:30), Fri: 9 hours (07:30-16:30)
            // Portuguese holidays excluded (simplified for now)
            
            int workDaysCount = 0;
            int fridayCount = 0;

            for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
            {
                var date = new DateTime(year, month, day);
                var dayOfWeek = date.DayOfWeek;

                if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                {
                    if (dayOfWeek == DayOfWeek.Friday)
                    {
                        fridayCount++;
                    }
                    else
                    {
                        workDaysCount++;
                    }
                }
            }

            double totalHours = (workDaysCount * 10) + (fridayCount * 9);
            return totalHours * TOTAL_MACHINES;
        }

        public double CalculateDowntimeInWorkHours(int year, int month)
        {
            // This would aggregate maintenance record durations for the month
            // Simplified: calculates based on maintenance records in the specified month
            // Only counts time during work hours (Mon-Fri, 07:30-16:30)
            
            double totalDowntimeHours = 0;

            // Get all maintenance records asynchronously in sync context
            // For now, return 0 - will be populated when records are created
            return totalDowntimeHours;
        }

        public double CalculateAvailability(int year, int month)
        {
            var scheduledHours = CalculateMonthlyScheduledHours(year, month);
            var downtimeHours = CalculateDowntimeInWorkHours(year, month);
            
            if (scheduledHours == 0) return 0;
            
            var availability = (scheduledHours - downtimeHours) / scheduledHours;
            return Math.Round(availability, 4);
        }

        public async Task<Dictionary<string, object>> GetMonthlyMetricsAsync(int year, int month)
        {
            var scheduledHours = CalculateMonthlyScheduledHours(year, month);
            var downtimeHours = CalculateDowntimeInWorkHours(year, month);
            var availability = CalculateAvailability(year, month);

            var records = await _databaseService.GetAllMaintenanceRecordsAsync();
            
            // Count preventive and corrective maintenance
            int preventiveCount = records.Count(r => r.TryGetValue("type", out var t) && t?.ToString() == "preventiva");
            int correctiveCount = records.Count(r => r.TryGetValue("type", out var t) && t?.ToString() == "corretiva");
            
            // Calculate total costs from maintenance items
            double totalCosts = 0;
            
            var metrics = new Dictionary<string, object>
            {
                { "availability", availability },
                { "availabilityPercentage", $"{availability * 100:F2}%" },
                { "availabilityTarget", AVAILABILITY_TARGET },
                { "scheduledHours", scheduledHours },
                { "downtimeHours", downtimeHours },
                { "preventiveMaintenanceCount", preventiveCount },
                { "correctiveMaintenanceCount", correctiveCount },
                { "totalMachines", TOTAL_MACHINES },
                { "totalCosts", totalCosts },
                { "month", month },
                { "year", year }
            };

            return metrics;
        }
    }

    public class ServerDiagnosticsService : IServerDiagnosticsService
    {
        private readonly ILocalDatabaseService _databaseService;
        private bool _isOnline = true;

        public ServerDiagnosticsService(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<bool> SendHeartbeatAsync()
        {
            try
            {
                var diagnostics = new Dictionary<string, object>
                {
                    { "id", Guid.NewGuid().ToString() },
                    { "server_ip", GetLocalIpAddress() },
                    { "is_online", true },
                    { "last_heartbeat", DateTime.Now },
                    { "created_at", DateTime.Now },
                    { "updated_at", DateTime.Now }
                };

                await _databaseService.SaveAsync("system_diagnostics", diagnostics);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<string> GetServerStatusAsync()
        {
            return Task.FromResult(_isOnline ? "Online" : "Offline");
        }

        public async Task LogCommandAsync(string command)
        {
            try
            {
                var log = new Dictionary<string, object>
                {
                    { "id", Guid.NewGuid().ToString() },
                    { "command", command },
                    { "executed_at", DateTime.Now },
                    { "created_at", DateTime.Now }
                };

                await _databaseService.SaveAsync("system_diagnostics", log);
            }
            catch
            {
                // Log error silently
            }
        }

        private string GetLocalIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }



    public class AlertConfigurationService : IAlertConfigurationService
    {
        private readonly ILocalDatabaseService _databaseService;

        public AlertConfigurationService(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<Dictionary<string, object>> GetAlertSettingsAsync(string userId)
        {
            try
            {
                var settings = await _databaseService.GetAllAsync<Dictionary<string, object>>("alert_settings");
                var userSettings = settings.FirstOrDefault(s => s.TryGetValue("user_id", out var uid) && uid?.ToString() == userId);
                
                return userSettings ?? new Dictionary<string, object>
                {
                    { "user_id", userId },
                    { "alerts_enabled", false },
                    { "email_recipient", string.Empty },
                    { "alert_frequency", "daily" }
                };
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public async Task<bool> SaveAlertSettingsAsync(string userId, Dictionary<string, object> settings)
        {
            try
            {
                settings["user_id"] = userId;
                settings["updated_at"] = DateTime.Now;
                
                if (!settings.ContainsKey("id"))
                {
                    settings["id"] = Guid.NewGuid().ToString();
                    settings["created_at"] = DateTime.Now;
                }

                await _databaseService.SaveAsync("alert_settings", settings);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
