using System.Windows;
using System.Windows.Controls;
using TTXEquipamentos.ViewModels;
using Microsoft.Win32;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;

namespace TTXEquipamentos.Views.Pages
{
    public partial class MaintenanceReport : Page
    {
        public MaintenanceReport()
        {
            InitializeComponent();
            DataContext = new MaintenanceReportViewModel();
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MaintenanceReportViewModel;
            if (viewModel == null) return;

            // Open file dialog to select image
            var openFileDialog = new OpenFileDialog
            {
                Title = "Selecionar Foto",
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp|Todos os arquivos|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var src = openFileDialog.FileName;
                    var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TTXEquipamentos", "photos");
                    Directory.CreateDirectory(appData);
                    var destFileName = System.Guid.NewGuid().ToString() + Path.GetExtension(src);
                    var dest = Path.Combine(appData, destFileName);
                    File.Copy(src, dest, true);

                    var photo = new Models.MaintenanceReportPhoto
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        FilePath = dest,
                        FileName = Path.GetFileName(src),
                        UploadedAt = System.DateTime.Now
                    };

                    viewModel.Photos.Add(photo);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Erro ao adicionar foto: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenPhoto(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Models.MaintenanceReportPhoto photo && !string.IsNullOrWhiteSpace(photo.FilePath))
            {
                try
                {
                    var psi = new ProcessStartInfo(photo.FilePath) { UseShellExecute = true };
                    Process.Start(psi);
                }
                catch { }
            }
        }
    }
}
