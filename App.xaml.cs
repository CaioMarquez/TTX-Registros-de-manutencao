using System;
using System.Windows;
using TTXEquipamentos.Services;
using TTXEquipamentos.Data;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace TTXEquipamentos
{
    public partial class App : Application
    {
        public static ServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            try
            {
                // Ensure logs directory exists
                string logsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                System.IO.Directory.CreateDirectory(logsDir);

                // Initialize Serilog - OTIMIZAÇÃO: removido WriteTo.Console() para reduzir overhead
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(System.IO.Path.Combine(logsDir, "app-.txt"), rollingInterval: RollingInterval.Day)
                    .CreateLogger();
                    
                Log.Information("===== APPLICATION STARTED =====");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing logger: {ex}", "Logger Error");
                throw;
            }

            this.DispatcherUnhandledException += (s, e) => 
            {
                Log.Fatal(e.Exception, "Unhandled UI exception");
                e.Handled = true; // Prevent immediate crash to see the log
                MessageBox.Show(e.Exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Log.Fatal((Exception)e.ExceptionObject, "Unhandled AppDomain exception");
                MessageBox.Show(((Exception)e.ExceptionObject).ToString(), "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Force pt-BR culture for formatting and DatePicker language
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pt-BR");
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pt-BR");
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), 
                    new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR")));

                InitializeializeDependencies();
                
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start application");
                MessageBox.Show($"Failed to start application:\n\n{ex}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void InitializeializeDependencies()
        {
            Log.Information("[App.xaml.cs] Initializing dependencies...");
            
            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<ILocalDatabaseService, JsonLocalDatabaseService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IMaintenanceCalculationsService, MaintenanceCalculationsService>();
            services.AddSingleton<IServerDiagnosticsService, ServerDiagnosticsService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IAlertConfigurationService, AlertConfigurationService>();
            services.AddSingleton<IImageCacheService, ImageCacheService>(); // Image caching for performance

            // ViewModels
            services.AddTransient<ViewModels.AuthViewModel>();
            services.AddTransient<ViewModels.DashboardViewModel>();
            services.AddTransient<ViewModels.MachinesViewModel>();
            services.AddTransient<ViewModels.MyOSViewModel>();
            services.AddTransient<ViewModels.NewOSViewModel>();
            services.AddTransient<ViewModels.HistoryViewModel>();
            services.AddTransient<ViewModels.UsersViewModel>();
            services.AddTransient<ViewModels.ExtraCostsViewModel>();
            services.AddTransient<ViewModels.ProfileViewModel>();
            services.AddTransient<ViewModels.IndicatorsViewModel>();
            services.AddTransient<ViewModels.AlertSettingsViewModel>();
            services.AddTransient<ViewModels.BackupsViewModel>();
            services.AddTransient<ViewModels.CalendarViewModel>();
            services.AddTransient<ViewModels.CollaboratorsViewModel>();

            ServiceProvider = services.BuildServiceProvider();
            Log.Information("[App.xaml.cs] ServiceProvider built");

            // Initialize database
            var dbService = ServiceProvider.GetRequiredService<ILocalDatabaseService>();
            dbService.Initialize();
            Log.Information("[App.xaml.cs] Database initialized");

            // TEST: Verify data can be loaded
            try
            {
                var testRecords = dbService.GetAllAsync<Models.MaintenanceRecord>("maintenance_records").Result;
                Log.Information("[App.xaml.cs] Test load: {RecordsCount} maintenance records loaded", testRecords?.Count ?? 0);
                
                var testMachines = dbService.GetAllAsync<Models.Machine>("machines").Result;
                Log.Information("[App.xaml.cs] Test load: {MachinesCount} machines loaded", testMachines?.Count ?? 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[App.xaml.cs] Test load error: {ErrorMessage}", ex.Message);
            }

            // If started with self-test flag, instantiate NewOSViewModel and run its self-test filter
            try
            {
                var args = Environment.GetCommandLineArgs();
                if (args != null && args.Contains("--selftest-filter"))
                {
                    Log.Information("[App.xaml.cs] Self-test flag detected, running NewOSViewModel.SelfTestFilterAsync");
                    var vm = new ViewModels.NewOSViewModel(dbService, ServiceProvider.GetService(typeof(IAuthenticationService)) as IAuthenticationService, ServiceProvider.GetService(typeof(INavigationService)) as INavigationService);
                    vm.SelfTestFilterAsync().GetAwaiter().GetResult();
                    Log.Information("[App.xaml.cs] Self-test filter completed");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[App.xaml.cs] Self-test filter failed: {Message}", ex.Message);
            }

            Log.Information("[App.xaml.cs] Dependencies initialized successfully");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Log.CloseAndFlush();
        }
    }
}
