using System;
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
    public class UserRoleViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;
        public Profile Profile { get; set; }
        public string? Role { get; set; }

        public UserRoleViewModel(ILocalDatabaseService databaseService, Profile profile, string? role)
        {
            _databaseService = databaseService;
            Profile = profile;
            Role = role;
        }
    }

    public class UsersViewModel : ViewModelBase
    {
        private readonly ILocalDatabaseService _databaseService;
        private List<UserRoleViewModel> _allUsersWithRoles = new();
        
        private ObservableCollection<UserRoleViewModel> _approvedUsers = new();
        private ICollectionView _approvedUsersView;
        private ObservableCollection<UserRoleViewModel> _pendingUsers = new();
        private ICollectionView _pendingUsersView;
        
        private string _searchText = string.Empty;
        private bool _isLoading;

        // Dialog states
        private bool _isRoleDialogOpen;
        private bool _isPasswordDialogOpen;
        private bool _isDeleteDialogOpen;
        private UserRoleViewModel? _selectedUserForRole;
        private string _selectedRole = string.Empty;
        private string _newPassword = string.Empty;
        private UserRoleViewModel? _userToDelete;
        private bool _isApprovingUser;

        // Collections and views
        public ObservableCollection<UserRoleViewModel> ApprovedUsers
        {
            get => _approvedUsers;
            set
            {
                SetProperty(ref _approvedUsers, value);
                _approvedUsersView = CollectionViewSource.GetDefaultView(_approvedUsers);
                if (_approvedUsersView != null) _approvedUsersView.Filter = FilterUsers;
            }
        }
        public ICollectionView ApprovedUsersView => _approvedUsersView;

        public ObservableCollection<UserRoleViewModel> PendingUsers
        {
            get => _pendingUsers;
            set
            {
                SetProperty(ref _pendingUsers, value);
                _pendingUsersView = CollectionViewSource.GetDefaultView(_pendingUsers);
                if (_pendingUsersView != null) _pendingUsersView.Filter = FilterUsers;
            }
        }
        public ICollectionView PendingUsersView => _pendingUsersView;

        // Search and filtering
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _approvedUsersView?.Refresh();
                    _pendingUsersView?.Refresh();
                }
            }
        }

        // Dialog properties
        public bool IsRoleDialogOpen { get => _isRoleDialogOpen; set => SetProperty(ref _isRoleDialogOpen, value); }
        public bool IsPasswordDialogOpen { get => _isPasswordDialogOpen; set => SetProperty(ref _isPasswordDialogOpen, value); }
        public bool IsDeleteDialogOpen { get => _isDeleteDialogOpen; set => SetProperty(ref _isDeleteDialogOpen, value); }
        public UserRoleViewModel? SelectedUserForRole { get => _selectedUserForRole; set => SetProperty(ref _selectedUserForRole, value); }
        public string SelectedRole { get => _selectedRole; set => SetProperty(ref _selectedRole, value); }
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
        public UserRoleViewModel? UserToDelete { get => _userToDelete; set => SetProperty(ref _userToDelete, value); }
        public bool IsApprovingUser { get => _isApprovingUser; set => SetProperty(ref _isApprovingUser, value); }

        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        // Commands
        public ICommand LoadUsersCommand { get; }
        public ICommand OpenRoleDialogCommand { get; }
        public ICommand OpenApproveDialogCommand { get; }
        public ICommand OpenPasswordDialogCommand { get; }
        public ICommand OpenDeleteDialogCommand { get; }
        public ICommand AssignRoleCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ResetPasswordCommand { get; }
        public ICommand CloseDialogCommand { get; }

        public UsersViewModel(ILocalDatabaseService databaseService)
        {
            _databaseService = databaseService;

            LoadUsersCommand = new RelayCommand(async (_) => await LoadUsersAsync());
            OpenRoleDialogCommand = new RelayCommand(p => OpenRoleDialog(p as UserRoleViewModel));
            OpenApproveDialogCommand = new RelayCommand(p => OpenApproveDialog(p as UserRoleViewModel));
            OpenPasswordDialogCommand = new RelayCommand(p => OpenPasswordDialog(p as UserRoleViewModel));
            OpenDeleteDialogCommand = new RelayCommand(p => OpenDeleteDialog(p as UserRoleViewModel));
            AssignRoleCommand = new RelayCommand(async _ => await AssignRoleAsync());
            DeleteUserCommand = new RelayCommand(async _ => await ExecuteDeleteUserAsync());
            ResetPasswordCommand = new RelayCommand(_ => HandlePasswordReset());
            CloseDialogCommand = new RelayCommand(p => CloseDialog(p as string));

            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var profiles = await _databaseService.GetAllAsync<Profile>("profiles");
                var userRoles = await _databaseService.GetAllAsync<UserRole>("user_roles");

                _allUsersWithRoles.Clear();
                foreach (var profile in profiles.OrderBy(x => x.Name))
                {
                    var role = userRoles.FirstOrDefault(r => r.UserId == profile.Id);
                    var roleValue = role?.Role;

                    _allUsersWithRoles.Add(new UserRoleViewModel(_databaseService, profile, roleValue));
                }

                RefreshUserCollections();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro: {ex.Message}");
            }
            finally { IsLoading = false; }
        }

        private void RefreshUserCollections()
        {
            var approved = _allUsersWithRoles.Where(u => !string.IsNullOrEmpty(u.Role)).ToList();
            var pending = _allUsersWithRoles.Where(u => string.IsNullOrEmpty(u.Role)).ToList();

            ApprovedUsers = new ObservableCollection<UserRoleViewModel>(approved);
            PendingUsers = new ObservableCollection<UserRoleViewModel>(pending);

            _approvedUsersView = CollectionViewSource.GetDefaultView(ApprovedUsers);
            _approvedUsersView.Filter = FilterUsers;
            OnPropertyChanged(nameof(ApprovedUsersView));

            _pendingUsersView = CollectionViewSource.GetDefaultView(PendingUsers);
            _pendingUsersView.Filter = FilterUsers;
            OnPropertyChanged(nameof(PendingUsersView));
        }

        private bool FilterUsers(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (obj is UserRoleViewModel u)
            {
                var search = SearchText.ToLower();
                return (u.Profile?.Name?.ToLower().Contains(search) == true) ||
                       (u.Profile?.Email?.ToLower().Contains(search) == true);
            }
            return false;
        }

        private void OpenRoleDialog(UserRoleViewModel? user)
        {
            if (user == null) return;
            SelectedUserForRole = user;
            SelectedRole = user.Role ?? "";
            IsApprovingUser = false;
            IsRoleDialogOpen = true;
        }

        private void OpenApproveDialog(UserRoleViewModel? user)
        {
            if (user == null) return;
            SelectedUserForRole = user;
            SelectedRole = "tecnico"; // default role
            IsApprovingUser = true;
            IsRoleDialogOpen = true;
        }

        private void OpenPasswordDialog(UserRoleViewModel? user)
        {
            if (user == null) return;
            SelectedUserForRole = user;
            NewPassword = "";
            IsPasswordDialogOpen = true;
        }

        private void OpenDeleteDialog(UserRoleViewModel? user)
        {
            if (user == null) return;
            UserToDelete = user;
            IsDeleteDialogOpen = true;
        }

        private async Task AssignRoleAsync()
        {
            if (SelectedUserForRole == null || string.IsNullOrEmpty(SelectedRole)) return;

            try
            {
                // Delete existing role if any
                var existingRoles = await _databaseService.GetAllAsync<UserRole>("user_roles");
                var roleEntry = existingRoles.FirstOrDefault(r => r.UserId == SelectedUserForRole.Profile.Id);

                if (roleEntry != null && roleEntry.Id != null)
                {
                    await _databaseService.DeleteAsync<UserRole>("user_roles", roleEntry.Id);
                }

                // Save new role
                var newRole = new UserRole
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = SelectedUserForRole.Profile.Id,
                    Role = SelectedRole
                };

                await _databaseService.SaveAsync("user_roles", newRole);

                SelectedUserForRole.Role = SelectedRole;
                _allUsersWithRoles.FirstOrDefault(u => u.Profile.Id == SelectedUserForRole.Profile.Id)!.Role = SelectedRole;
                RefreshUserCollections();

                System.Windows.MessageBox.Show(IsApprovingUser ? "Usuário aprovado!" : "Nível de acesso atualizado!");
                CloseDialog("role");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private void HandlePasswordReset()
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                System.Windows.MessageBox.Show("A senha deve ter no mínimo 6 caracteres");
                return;
            }

            System.Windows.MessageBox.Show("Nota: O usuário precisa alterar a senha através do seu próprio perfil.");
            CloseDialog("password");
        }

        private async Task ExecuteDeleteUserAsync()
        {
            if (UserToDelete == null) return;

            try
            {
                // Delete user roles first
                var roles = await _databaseService.GetAllAsync<UserRole>("user_roles");
                var userRoles = roles.Where(r => r.UserId == UserToDelete.Profile.Id).ToList();

                foreach (var role in userRoles)
                {
                    if (role.Id != null)
                    {
                        await _databaseService.DeleteAsync<UserRole>("user_roles", role.Id);
                    }
                }

                // Delete user profile
                await _databaseService.DeleteAsync<Profile>("profiles", UserToDelete.Profile.Id ?? "");

                _allUsersWithRoles.Remove(UserToDelete);
                RefreshUserCollections();

                System.Windows.MessageBox.Show("Usuário deletado com sucesso!");
                CloseDialog("delete");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private void CloseDialog(string? dialogType)
        {
            switch (dialogType?.ToLower())
            {
                case "role":
                    IsRoleDialogOpen = false;
                    SelectedUserForRole = null;
                    SelectedRole = "";
                    IsApprovingUser = false;
                    break;
                case "password":
                    IsPasswordDialogOpen = false;
                    SelectedUserForRole = null;
                    NewPassword = "";
                    break;
                case "delete":
                    IsDeleteDialogOpen = false;
                    UserToDelete = null;
                    break;
            }
        }
    }
}
