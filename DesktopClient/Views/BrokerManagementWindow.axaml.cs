using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using InvestmentAnalyzer.State;
using ReactiveUI;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class BrokerManagementWindow : ReactiveWindow<BrokerManagementWindowViewModel> {
		public BrokerManagementWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(_ => {
				ViewModel?.ShowAddBrokerWindow.RegisterHandler(ShowAddBrokerWindow);
			});
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		async Task ShowAddBrokerWindow(InteractionContext<Unit, BrokerState?> interaction) {
			var addBrokerDialog = new AddBrokerWindow {
				ViewModel = new AddBrokerWindowViewModel()
			};
			var result = await addBrokerDialog.ShowDialog<BrokerState?>(this);
			interaction.SetOutput(result);
		}
	}
}