using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TTXEquipamentos.Models;
using TTXEquipamentos.Services;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class CollaboratorsViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;

        public ObservableCollection<Collaborator> Profiles { get; } = new();

        private Collaborator? _selectedProfile;
        public Collaborator? SelectedProfile { get => _selectedProfile; set => SetProperty(ref _selectedProfile, value); }

        private string _newName = "";
        public string NewName { get => _newName; set => SetProperty(ref _newName, value); }

        private string _newFunction = "";
        public string NewFunction { get => _newFunction; set => SetProperty(ref _newFunction, value); }

        public ICommand LoadCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public CollaboratorsViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => SelectedProfile != null);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedProfile != null);

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                Profiles.Clear();
                var profiles = await _databaseService.GetAllAsync<Collaborator>("collaborators");
                foreach (var p in profiles.OrderBy(x => x.Name)) Profiles.Add(p);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar colaboradores: {ex.Message}");
            }
        }

        private async Task AddAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                MessageBox.Show("Informe o nome do colaborador.");
                return;
            }

            var collaborator = new Collaborator
            {
                Id = Guid.NewGuid().ToString(),
                Name = NewName,
                Function = string.IsNullOrWhiteSpace(NewFunction) ? null : NewFunction,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var ok = await _databaseService.SaveAsync("collaborators", collaborator);
            if (ok)
            {
                Profiles.Add(collaborator);
                NewName = "";
                NewFunction = "";
            }
            else MessageBox.Show("Falha ao salvar colaborador.");
        }

        private async Task SaveAsync()
        {
            if (SelectedProfile == null) return;
            SelectedProfile.UpdatedAt = DateTime.Now;
            var ok = await _databaseService.SaveAsync("collaborators", SelectedProfile);
            if (!ok) MessageBox.Show("Falha ao salvar colaborador.");
            else await LoadAsync();
        }

        private async Task DeleteAsync()
        {
            if (SelectedProfile == null) return;
            var res = MessageBox.Show($"Excluir {SelectedProfile.Name}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;
            var ok = await _databaseService.DeleteAsync<Collaborator>("collaborators", SelectedProfile.Id ?? string.Empty);
            if (ok)
            {
                Profiles.Remove(SelectedProfile);
                SelectedProfile = null;
            }
            else MessageBox.Show("Falha ao excluir colaborador.");
        }
    }
}
