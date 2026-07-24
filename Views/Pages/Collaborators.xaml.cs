using System.Windows.Controls;
using TTXEquipamentos.Services;
using TTXEquipamentos.ViewModels;

namespace TTXEquipamentos.Views.Pages
{
    public partial class Collaborators : Page
    {
        public Collaborators()
        {
            InitializeComponent();

            var db = App.ServiceProvider?.GetService(typeof(ILocalDatabaseService)) as ILocalDatabaseService;
            DataContext = new CollaboratorsViewModel(db!);
        }
    }
}
