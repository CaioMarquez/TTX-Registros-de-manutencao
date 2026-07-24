using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Wpf;

namespace TTXEquipamentos.Views.Pages
{
    public partial class Indicators : Page
    {
        public Indicators()
        {
            InitializeComponent();
            var vm = App.ServiceProvider?.GetService(typeof(ViewModels.IndicatorsViewModel));
            if (vm != null) this.DataContext = vm;

            SetupPlotController(MonthlyCostsPlotView);
            SetupPlotController(AnnualCorrectivePlotView);
            SetupPlotController(MachineCostsPlotView);
        }

        private void SetupPlotController(PlotView plotView)
        {
            if (plotView == null) return;

            var controller = new PlotController();
            controller.UnbindAll();
            controller.Bind(new OxyMouseDownGesture(OxyMouseButton.Left, OxyModifierKeys.None, 1), OxyPlot.PlotCommands.PanAt);
            plotView.Controller = controller;
        }
    }
}