using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using TTXEquipamentos.Models;
using TTXEquipamentos.Services;
using TTXEquipamentos.Utilities;

namespace TTXEquipamentos.ViewModels
{
    public class ExtraCostsViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;
        private readonly IAuthenticationService _authService;
        
        private ObservableCollection<ExtraCost> _costs = new();
        private ICollectionView _costsView;
        private ObservableCollection<Contractor> _contractors = new();
        
        private string _newContractorName = string.Empty;
        private string _newContractorSpecialty = string.Empty;
        
        private Contractor? _selectedContractor;
        private string _costAmount = string.Empty;
        private DateTime _costServiceDate = DateTime.Now;
        private string _costDescription = string.Empty;
        
        private ExtraCost? _editingCost;
        
        private bool _isAdmin;
        private double _totalCosts;

        public ObservableCollection<ExtraCost> Costs
        {
            get => _costs;
            set
            {
                SetProperty(ref _costs, value);
                
                // Recria a View sempre que a lista for carregada
                _costsView = CollectionViewSource.GetDefaultView(_costs);
                if (_costsView != null)
                {
                    _costsView.SortDescriptions.Clear();
                    _costsView.SortDescriptions.Add(new SortDescription(nameof(ExtraCost.ServiceDate), ListSortDirection.Descending));
                }
                OnPropertyChanged(nameof(CostsView));
            }
        }
        
        public ICollectionView CostsView => _costsView;

        public ObservableCollection<Contractor> Contractors
        {
            get => _contractors;
            set => SetProperty(ref _contractors, value);
        }

        public string NewContractorName
        {
            get => _newContractorName;
            set => SetProperty(ref _newContractorName, value);
        }

        public string NewContractorSpecialty
        {
            get => _newContractorSpecialty;
            set => SetProperty(ref _newContractorSpecialty, value);
        }

        public Contractor? SelectedContractor
        {
            get => _selectedContractor;
            set => SetProperty(ref _selectedContractor, value);
        }

        public string CostAmount
        {
            get => _costAmount;
            set => SetProperty(ref _costAmount, value);
        }

        public DateTime CostServiceDate
        {
            get => _costServiceDate;
            set => SetProperty(ref _costServiceDate, value);
        }

        public string CostDescription
        {
            get => _costDescription;
            set => SetProperty(ref _costDescription, value);
        }

        public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }

        public double TotalCosts
        {
            get => _totalCosts;
            set => SetProperty(ref _totalCosts, value);
        }

        public string TotalCostsFormatted => TotalCosts.ToString("C0");

        public ICommand LoadCommand { get; }
        public ICommand AddContractorCommand { get; }
        public ICommand AddCostCommand { get; }
        public ICommand EditCostCommand { get; }
        public ICommand DeleteCostCommand { get; }
        public ICommand DeleteContractorCommand { get; }

        public ExtraCostsViewModel(ILocalDatabaseService databaseService, IAuthenticationService authService)
        {
            _databaseService = databaseService;
            _authService = authService;
            IsAdmin = authService.GetCurrentUserRole() == "admin";

            LoadCommand = new RelayCommand(async (_) => await LoadDataAsync());
            AddContractorCommand = new RelayCommand(async (_) => await AddContractorAsync());
            AddCostCommand = new RelayCommand(async (_) => await AddCostAsync());
            EditCostCommand = new RelayCommand(async (param) => await EditCostAsync(param as ExtraCost));
            DeleteCostCommand = new RelayCommand(async (param) => await DeleteCostAsync(param as ExtraCost));
            DeleteContractorCommand = new RelayCommand(async (param) => await DeleteContractorAsync(param as Contractor));

            Costs = new ObservableCollection<ExtraCost>();

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                // Load contractors
                var allContractors = await _databaseService.GetAllAsync<Contractor>("contractors") ?? new List<Contractor>();
                Contractors.Clear();
                foreach (var c in allContractors.Where(x => x.Active).OrderBy(x => x.Name))
                    Contractors.Add(c);

                // Load costs
                var allCosts = await _databaseService.GetAllAsync<ExtraCost>("extra_costs") ?? new List<ExtraCost>();
                
                // O segredo do XAML atualizar está nesta linha chamando o "set"
                Costs = new ObservableCollection<ExtraCost>(allCosts);

                CalculateTotalCosts();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao carregar dados: {ex.Message}");
            }
            finally { IsLoading = false; }
        }

        private async Task AddContractorAsync()
        {
            if (string.IsNullOrWhiteSpace(NewContractorName))
            {
                System.Windows.MessageBox.Show("Informe o nome do prestador");
                return;
            }

            try
            {
                var contractor = new Contractor
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = NewContractorName.Trim(),
                    Specialty = string.IsNullOrWhiteSpace(NewContractorSpecialty) ? null : NewContractorSpecialty.Trim(),
                    Active = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _databaseService.SaveAsync("contractors", contractor);
                Contractors.Add(contractor);
                
                Contractors = new ObservableCollection<Contractor>(Contractors.OrderBy(x => x.Name));

                NewContractorName = string.Empty;
                NewContractorSpecialty = string.Empty;

                System.Windows.MessageBox.Show("Prestador cadastrado com sucesso!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private async Task AddCostAsync()
        {
            if (SelectedContractor == null)
            {
                System.Windows.MessageBox.Show("Selecione um prestador");
                return;
            }

            if (string.IsNullOrWhiteSpace(CostAmount) || !double.TryParse(CostAmount.Replace(",", "."), out var amount) || amount <= 0)
            {
                System.Windows.MessageBox.Show("Informe um valor válido");
                return;
            }

            try
            {
                var cost = new ExtraCost
                {
                    Id = Guid.NewGuid().ToString(),
                    ContractorId = SelectedContractor.Id,
                    ContractorName = SelectedContractor.Name,
                    Amount = amount,
                    ServiceDate = CostServiceDate,
                    InvoiceDate = DateTime.Now,
                    Description = string.IsNullOrWhiteSpace(CostDescription) ? null : CostDescription.Trim(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _databaseService.SaveAsync("extra_costs", cost);
                
                Costs.Add(cost);
                CalculateTotalCosts();

                SelectedContractor = null;
                CostAmount = string.Empty;
                CostServiceDate = DateTime.Now;
                CostDescription = string.Empty;

                System.Windows.MessageBox.Show("Custo registrado com sucesso!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private async Task EditCostAsync(ExtraCost? cost)
        {
            if (cost == null) return;

            _editingCost = cost;
            SelectedContractor = Contractors.FirstOrDefault(c => c.Id == cost.ContractorId);
            CostAmount = cost.Amount.ToString().Replace(".", ",");
            CostServiceDate = cost.ServiceDate;
            CostDescription = cost.Description ?? string.Empty;

            System.Windows.MessageBox.Show("Editar custo - implementar dialog de edição");
        }

        private async Task DeleteCostAsync(ExtraCost? cost)
        {
            if (cost == null) return;

            var result = System.Windows.MessageBox.Show("Tem certeza que deseja remover este custo?", "Confirmar", System.Windows.MessageBoxButton.YesNo);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                await _databaseService.DeleteAsync<ExtraCost>("extra_costs", cost.Id ?? "");
                Costs.Remove(cost);
                CalculateTotalCosts();
                System.Windows.MessageBox.Show("Custo removido!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private async Task DeleteContractorAsync(Contractor? contractor)
        {
            if (contractor == null) return;

            var result = System.Windows.MessageBox.Show("Tem certeza que deseja remover este prestador?", "Confirmar", System.Windows.MessageBoxButton.YesNo);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                contractor.Active = false;
                contractor.UpdatedAt = DateTime.Now;
                await _databaseService.SaveAsync("contractors", contractor);
                Contractors.Remove(contractor);
                System.Windows.MessageBox.Show("Prestador removido!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private void CalculateTotalCosts()
        {
            int currentYear = DateTime.Now.Year;
            TotalCosts = Costs
                .Where(x => x.ServiceDate.Year == currentYear)
                .Sum(x => x.Amount);
            OnPropertyChanged(nameof(TotalCostsFormatted));
        }
    }
}