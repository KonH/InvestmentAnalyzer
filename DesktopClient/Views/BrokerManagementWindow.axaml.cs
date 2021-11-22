using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using ReactiveUI;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class BrokerManagementWindow : ReactiveWindow<BrokerManagementWindowViewModel> {
		public BrokerManagementWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(d => {
				ViewModel?.ShowAddBrokerWindow.RegisterHandler(this.ShowAddBrokerWindow);
			});
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}