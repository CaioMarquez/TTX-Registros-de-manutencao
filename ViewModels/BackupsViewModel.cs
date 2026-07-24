using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class BackupsViewModel : ViewModelBase
    {
        private string _statusMessage = string.Empty;
        private string _dataFolder;

        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string DataFolder { get => _dataFolder; set => SetProperty(ref _dataFolder, value); }

        public ICommand BackupCommand { get; }
        public ICommand RestoreCommand { get; }

        public BackupsViewModel()
        {
            _dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ttx-dados");
            BackupCommand = new RelayCommand(async (_) => await DoBackupAsync());
            RestoreCommand = new RelayCommand(async (_) => await DoRestoreAsync());
        }

        private async Task DoBackupAsync()
        {
            await ExecuteAsync(async () =>
            {
                var dlg = new SaveFileDialog
                {
                    FileName = $"backup_ttx_{DateTime.Now:yyyyMMdd_HHmm}.zip",
                    Filter = "Arquivo ZIP|*.zip"
                };

                if (dlg.ShowDialog() == true)
                {
                    if (File.Exists(dlg.FileName)) File.Delete(dlg.FileName);
                    System.IO.Compression.ZipFile.CreateFromDirectory(_dataFolder, dlg.FileName);
                    StatusMessage = $"✓ Backup salvo em: {dlg.FileName}";
                }
                await Task.CompletedTask;
            });
        }

        private async Task DoRestoreAsync()
        {
            await ExecuteAsync(async () =>
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "Arquivo ZIP|*.zip"
                };

                if (dlg.ShowDialog() == true)
                {
                    var confirm = System.Windows.MessageBox.Show(
                        "ATENÇÃO: Isso vai substituir todos os dados atuais. Deseja continuar?",
                        "Confirmar Restauração",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);

                    if (confirm == System.Windows.MessageBoxResult.Yes)
                    {
                        if (Directory.Exists(_dataFolder)) Directory.Delete(_dataFolder, true);
                        System.IO.Compression.ZipFile.ExtractToDirectory(dlg.FileName, _dataFolder);
                        StatusMessage = "✓ Dados restaurados com sucesso! Reinicie o aplicativo.";
                    }
                }
                await Task.CompletedTask;
            });
        }
    }
}
