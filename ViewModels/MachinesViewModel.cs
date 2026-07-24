using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TTXEquipamentos.Models;
using TTXEquipamentos.Services;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class MachinesViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;

        private ObservableCollection<Machine> _machines = new();
        private ICollectionView _machinesView;

        private string _searchText = string.Empty;

        // Dialog state
        private bool _isDialogOpen;
        private bool _isEditing;
        private string _dialogTitle = "Nova Máquina";

        // Form fields
        private string _formName = "";
        private string _formTag = "";
        private string _formArea = "";
        private string _formType = "eletrica";
        private string? _editingMachineId;

        // Validation errors
        private string _nameError = "";
        private string _tagError = "";
        private string _areaError = "";

        // Properties
        public ObservableCollection<Machine> Machines
        {
            get => _machines;
            set
            {
                SetProperty(ref _machines, value);
                _machinesView = CollectionViewSource.GetDefaultView(_machines);
                if (_machinesView != null)
                    _machinesView.Filter = FilterMachines;
            }
        }

        public ICollectionView MachinesView => _machinesView;

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) _machinesView?.Refresh(); }
        }

        // Dialog properties
        public bool IsDialogOpen { get => _isDialogOpen; set => SetProperty(ref _isDialogOpen, value); }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
        public string DialogTitle { get => _dialogTitle; set => SetProperty(ref _dialogTitle, value); }

        // Form properties
        public string FormName { get => _formName; set { SetProperty(ref _formName, value); NameError = ""; } }
        public string FormTag { get => _formTag; set { SetProperty(ref _formTag, value); TagError = ""; } }
        public string FormArea { get => _formArea; set { SetProperty(ref _formArea, value); AreaError = ""; } }
        public string FormType { get => _formType; set => SetProperty(ref _formType, value); }

        // Validation error properties
        public string NameError { get => _nameError; set => SetProperty(ref _nameError, value); }
        public string TagError { get => _tagError; set => SetProperty(ref _tagError, value); }
        public string AreaError { get => _areaError; set => SetProperty(ref _areaError, value); }

        // Commands
        public ICommand OpenNewDialogCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public MachinesViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            OpenNewDialogCommand = new RelayCommand(_ => OpenNewDialog());
            SaveCommand = new RelayCommand(async _ => await SaveMachineAsync());
            CancelCommand = new RelayCommand(_ => CloseDialog());
            EditCommand = new RelayCommand(param => OpenEditDialog(param as Machine));
            DeleteCommand = new RelayCommand(async param => await DeleteMachineAsync(param as Machine));

            _ = LoadMachinesAsync();
        }

        private void OpenNewDialog()
        {
            _editingMachineId = null;
            IsEditing = false;
            DialogTitle = "Nova Máquina";
            FormName = "";
            FormTag = "";
            FormArea = "";
            FormType = "eletrica";
            NameError = "";
            TagError = "";
            AreaError = "";
            IsDialogOpen = true;
        }

        private void OpenEditDialog(Machine? machine)
        {
            if (machine == null) return;
            _editingMachineId = machine.Id;
            IsEditing = true;
            DialogTitle = "Editar Máquina";
            FormName = machine.Name ?? "";
            FormTag = machine.Tag ?? "";
            FormArea = machine.Area ?? "";
            FormType = machine.Type ?? "eletrica";
            NameError = "";
            TagError = "";
            AreaError = "";
            IsDialogOpen = true;
        }

        private void CloseDialog()
        {
            IsDialogOpen = false;
            _editingMachineId = null;
        }

        private bool ValidateForm()
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(FormName) || FormName.Length < 2)
            {
                NameError = "Nome deve ter no mínimo 2 caracteres";
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(FormTag))
            {
                TagError = "Tag é obrigatória";
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(FormArea))
            {
                AreaError = "Área é obrigatória";
                valid = false;
            }

            return valid;
        }

        private async Task SaveMachineAsync()
        {
            if (!ValidateForm()) return;

            try
            {
                if (IsEditing && _editingMachineId != null)
                {
                    // Update existing
                    var machine = Machines.FirstOrDefault(m => m.Id == _editingMachineId);
                    if (machine != null)
                    {
                        machine.Name = FormName;
                        machine.Tag = FormTag;
                        machine.Area = FormArea;
                        machine.Type = FormType;
                        machine.UpdatedAt = DateTime.Now;
                        await _databaseService.SaveAsync("machines", machine);
                    }
                    MessageBox.Show("Máquina atualizada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Check duplicate tag
                    if (Machines.Any(m => m.Tag?.Equals(FormTag, StringComparison.OrdinalIgnoreCase) == true))
                    {
                        TagError = "Já existe uma máquina com esta tag.";
                        return;
                    }

                    // Create new
                    var newMachine = new Machine
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = FormName,
                        Tag = FormTag,
                        Area = FormArea,
                        Type = FormType,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    await _databaseService.SaveAsync("machines", newMachine);
                    MessageBox.Show("Máquina cadastrada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CloseDialog();
                await LoadMachinesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteMachineAsync(Machine? machine)
        {
            if (machine == null) return;

            var result = MessageBox.Show(
                $"Tem certeza que deseja remover a máquina \"{machine.Name} ({machine.Tag})\"?",
                "Confirmar Exclusão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _databaseService.DeleteAsync<Machine>("machines", machine.Id!);
                    MessageBox.Show("Máquina removida com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadMachinesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao remover: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task LoadMachinesAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var machines = await _databaseService.GetAllAsync<Machine>("machines");
                Machines.Clear();
                foreach (var machine in machines.OrderBy(m => m.Name))
                    Machines.Add(machine);

                _machinesView = CollectionViewSource.GetDefaultView(Machines);
                _machinesView.Filter = FilterMachines;
                OnPropertyChanged(nameof(MachinesView));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar máquinas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool FilterMachines(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (obj is Machine machine)
            {
                var s = SearchText.ToLower();
                return (machine.Tag?.ToLower().Contains(s) == true) ||
                       (machine.Name?.ToLower().Contains(s) == true) ||
                       (machine.Area?.ToLower().Contains(s) == true);
            }
            return false;
        }
    }
}
