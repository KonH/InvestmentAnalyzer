using System.Reactive;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using ReactiveUI;
using Splat;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public class DashboardWindow : ReactiveWindow<DashboardWindowViewModel> {
		public DashboardWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(_ => {
				ViewModel?.CloseWindow.RegisterHandler(this.CloseWindow);
				ViewModel?.ShowAssetStateWindow.RegisterHandler(ShowAssetStateWindow);
				ViewModel?.ShowBrokerManagementWindow.RegisterHandler(ShowBrokerManagementWindow);
				ViewModel?.ShowImportManagementWindow.RegisterHandler(ShowImportManagementWindow);
			});
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		void ShowAssetStateWindow(InteractionContext<Unit, Unit> interaction) {
			var assetStateWindow = new AssetStateWindow {
				ViewModel = new AssetStateWindowViewModel(Locator.Current.GetService<StateManager>())
			};
			assetStateWindow.Show(this);
			interaction.SetOutput(Unit.Default);
		}

		void ShowBrokerManagementWindow(InteractionContext<Unit, Unit> interaction) {
			var assetStateWindow = new BrokerManagementWindow {
				ViewModel = new BrokerManagementWindowViewModel(Locator.Current.GetService<StateManager>())
			};
			assetStateWindow.Show(this);
			interaction.SetOutput(Unit.Default);
		}

		void ShowImportManagementWindow(InteractionContext<Unit, Unit> interaction) {
			var assetStateWindow = new ImportManagementWindow {
				ViewModel = new ImportManagementWindowViewModel(Locator.Current.GetService<StateManager>())
			};
			assetStateWindow.Show(this);
			interaction.SetOutput(Unit.Default);
		}
	}
}