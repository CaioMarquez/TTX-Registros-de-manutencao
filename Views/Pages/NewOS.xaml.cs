using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TTXEquipamentos.Services;
using TTXEquipamentos.ViewModels;
using Microsoft.Win32;
using TTXEquipamentos.Models;
using System.IO;
using System.Diagnostics;

namespace TTXEquipamentos.Views.Pages
{
    public partial class NewOS : Page
    {
        private bool _isUpdatingMachineText;

        public NewOS()
        {
            InitializeComponent();
            var db = App.ServiceProvider?.GetService(typeof(ILocalDatabaseService)) as ILocalDatabaseService;
            var auth = App.ServiceProvider?.GetService(typeof(IAuthenticationService)) as IAuthenticationService;
            var nav = App.ServiceProvider?.GetService(typeof(INavigationService)) as INavigationService;
            DataContext = new NewOSViewModel(db!, auth!, nav!);

            // Ensure ComboBox inner TextBox updates VM search text in all build modes
            this.Loaded += (s, e) =>
            {
                try
                {
                    var vm = DataContext as NewOSViewModel;
                    if (MachineComboBox != null)
                    {
                        var tb = FindTextBoxInVisualTree(MachineComboBox);
                        if (tb != null)
                        {
                            tb.TextChanged += (sender, args) =>
                            {
                                if (_isUpdatingMachineText)
                                    return;

                                if (vm != null)
                                {
                                    vm.MachineSearchText = tb.Text ?? "";
                                }
                            };
                        }
                    }
                }
                catch { }
            };
        }

        private void MachineComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (MachineComboBox != null)
            {
                MachineComboBox.IsDropDownOpen = true;
                var tb = FindTextBoxInVisualTree(MachineComboBox);
                if (tb != null)
                {
                    tb.SelectAll();
                }
            }
        }

        private void MachineComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MachineComboBox != null && !MachineComboBox.IsDropDownOpen)
            {
                MachineComboBox.IsDropDownOpen = true;
                MachineComboBox.Focus();
                e.Handled = true;
            }
        }

        private void MachineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingMachineText)
                return;

            if (MachineComboBox?.SelectedItem is Machine selectedMachine)
            {
                var vm = DataContext as NewOSViewModel;
                if (vm == null)
                    return;

                var formatted = FormatMachineLabel(selectedMachine);
                _isUpdatingMachineText = true;
                try
                {
                    vm.MachineSearchText = formatted;
                    // Ensure VM selection is set (binding may update asynchronously)
                    vm.SelectedMachine = selectedMachine;
                    if (MachineComboBox.Text != formatted)
                        MachineComboBox.Text = formatted;
                    // Close dropdown and move focus so the text remains visible after clicking elsewhere
                    MachineComboBox.IsDropDownOpen = false;
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        var win = Window.GetWindow(this);
                        if (win != null)
                        {
                            FocusManager.SetFocusedElement(win, this);
                            Keyboard.Focus(this);
                        }
                    });
                }
                finally
                {
                    _isUpdatingMachineText = false;
                }
            }
        }

        private string FormatMachineLabel(Machine machine)
        {
            if (!string.IsNullOrWhiteSpace(machine.Tag) && !string.IsNullOrWhiteSpace(machine.Name))
                return $"{machine.Tag} - {machine.Name}";
            return machine.Tag ?? machine.Name ?? string.Empty;
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as NewOSViewModel;
            if (viewModel == null) return;

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

                    viewModel.Photos.Add(new MaintenanceReportPhoto
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        FilePath = dest,
                        FileName = Path.GetFileName(src),
                        UploadedAt = System.DateTime.Now
                    });
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Erro ao adicionar foto: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenPhoto(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is MaintenanceReportPhoto photo && !string.IsNullOrWhiteSpace(photo.FilePath))
            {
                try
                {
                    var psi = new ProcessStartInfo(photo.FilePath) { UseShellExecute = true };
                    Process.Start(psi);
                }
                catch { }
            }
        }

        private void TimeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits and colon
            var isDigit = char.IsDigit(e.Text[0]);
            var isColon = e.Text[0] == ':';
            e.Handled = !(isDigit || isColon);

            if (!e.Handled && sender is TextBox tb)
            {
                // Get current text with new character
                var currentText = tb.Text;
                var beforeCaret = currentText.Substring(0, tb.CaretIndex);
                var afterCaret = currentText.Substring(tb.CaretIndex);
                var newText = beforeCaret + e.Text + afterCaret;

                // Remove all non-digits to check length
                var digitsOnly = new string(newText.Where(char.IsDigit).ToArray());
                
                // Prevent entering more than 4 digits (HHmm)
                if (digitsOnly.Length > 4)
                {
                    e.Handled = true;
                    return;
                }

                // Handle different digit counts
                if (digitsOnly.Length == 2 && !newText.Contains(":"))
                {
                    // 2 digits: format as HH:
                    tb.Text = digitsOnly[0].ToString() + digitsOnly[1].ToString() + ":";
                    tb.CaretIndex = 3;
                    e.Handled = true;
                }
                else if (digitsOnly.Length == 3)
                {
                    // 3 digits: check if first digit > 2 (invalid hour)
                    var firstDigit = int.Parse(digitsOnly[0].ToString());
                    if (firstDigit > 2)
                    {
                        // Add 0 in front: 820 -> 0820 -> 08:20
                        digitsOnly = "0" + digitsOnly;
                        tb.Text = digitsOnly[0].ToString() + digitsOnly[1].ToString() + ":" + digitsOnly[2].ToString() + digitsOnly[3].ToString();
                        tb.CaretIndex = tb.Text.Length;
                        e.Handled = true;
                    }
                    // If 3 digits and first is <= 2, wait for 4th digit (don't format yet)
                    // This allows typing "15:15" correctly
                }
                else if (digitsOnly.Length == 4)
                {
                    // 4 digits: format as HH:mm
                    tb.Text = digitsOnly[0].ToString() + digitsOnly[1].ToString() + ":" + digitsOnly[2].ToString() + digitsOnly[3].ToString();
                    tb.CaretIndex = tb.Text.Length;
                    e.Handled = true;
                }
                // Auto-format: insert colon if user types it at wrong position
                else if (e.Text == ":" && digitsOnly.Length == 2)
                {
                    var formatted = digitsOnly[0].ToString() + digitsOnly[1].ToString() + ":";
                    tb.Text = formatted;
                    tb.CaretIndex = formatted.Length;
                    e.Handled = true;
                }
            }
        }

        private void DatePicker_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;

                if (sender is DatePicker dp)
                {
                    // Get the text from the DatePicker's internal TextBox
                    var textBox = GetDatePickerTextBox(dp);
                    if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        var inputText = textBox.Text;
                        if (TryParseDateInput(inputText, out var parsedDate))
                        {
                            dp.SelectedDate = parsedDate;
                        }
                        else
                        {
                            MessageBox.Show("Data inválida. Use o formato: DD/MM/YY ou DDMMYY", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
        }

        private TextBox? GetDatePickerTextBox(DatePicker dp)
        {
            // DatePicker contém um TextBox interno que podemos usar
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dp); i++)
            {
                var child = VisualTreeHelper.GetChild(dp, i);
                if (child is TextBox textBox)
                    return textBox;

                if (child is FrameworkElement fe)
                {
                    var result = FindTextBoxInVisualTree(fe);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        private TextBox? FindTextBoxInVisualTree(DependencyObject obj)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is TextBox textBox)
                    return textBox;

                if (child is DependencyObject depObj)
                {
                    var result = FindTextBoxInVisualTree(depObj);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        private bool TryParseDateInput(string input, out DateTime parsedDate)
        {
            parsedDate = DateTime.MinValue;

            // Remove common separators
            var cleaned = input.Replace("/", "").Replace("-", "").Replace(".", "").Trim();

            // Accept only digits
            if (!cleaned.All(char.IsDigit))
                return false;

            // Try different formats
            // Format: DDMMYY (6 digits)
            if (cleaned.Length == 6 && int.TryParse(cleaned, out var numericValue))
            {
                var day = int.Parse(cleaned.Substring(0, 2));
                var month = int.Parse(cleaned.Substring(2, 2));
                var year = int.Parse(cleaned.Substring(4, 2));

                // Convert 2-digit year to 4-digit year
                if (year <= DateTime.Now.Year % 100)
                    year += 2000;
                else
                    year += 1900;

                try
                {
                    parsedDate = new DateTime(year, month, day);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // Try standard date formats with DD/MM/YYYY or DD/MM/YY
            var formats = new[] { "dd/MM/yyyy", "dd/MM/yy", "d/M/yyyy", "d/M/yy", "dd-MM-yyyy", "dd-MM-yy", "ddMMyyyy", "ddMMyy" };
            if (DateTime.TryParseExact(input, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var result))
            {
                parsedDate = result;
                return true;
            }

            return false;
        }
    }
}