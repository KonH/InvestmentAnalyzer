using System.Reactive;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopClient.Views;
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
				ViewModel?.ShowAssetPlotWindow.RegisterHandler(ShowAssetPlotWindow);
				ViewModel?.ShowOperationsWindow.RegisterHandler(ShowOperationsWindow);
				ViewModel?.ShowBrokerManagementWindow.RegisterHandler(ShowBrokerManagementWindow);
				ViewModel?.ShowImportStateManagementWindow.RegisterHandler(ShowImportStateManagementWindow);
				ViewModel?.ShowImportOperationsManagementWindow.RegisterHandler(ShowImportOperationsManagementWindow);
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

		void ShowAssetPlotWindow(InteractionContext<Unit, Unit> interaction) {
			var assetPlotWindow = new AssetPlotWindow {
				ViewModel = new AssetPlotWindowViewModel(Locator.Current.GetService<StateManager>())
			};
			assetPlotWindow.Show(this);
			interaction.SetOutput(Unit.Default);
		}

		void ShowOperationsWindow(InteractionContext<Unit, Unit> interaction) {
			var operationsWindow = new OperationsWindow {
				ViewModel = new OperationsWindowViewModel(Locator.Current.GetService<StateManager>())
			};
			operationsWindow.Show(this);
			interaction.SetOutput(Unit.Default);
		}

		void ShowBrokerManagementWindow(InteractionContext<Unit, Unit> interaction) {
			var assetStateWindow = new BrokerManagementWindow {
				ViewModel = new BrokerManagementWindowViewModel(Locator.Current.GetService<StateManager>())
			};
			assetStateWindow.Show(this);
			interaction.SetOutput(Unit.Default);
		}

		void ShowImportStateManagementWindow(InteractionContext<Unit, Unit> interaction) {
			var assetStateWindow = new ImportStateManagementWindow {
				ViewModel = new ImportStateManagementWindowViewModel(Locator.Current.GetService<StateManager>())
			};
			assetStateWindow.Show(this);
			interaction.SetOutput(Unit.Default);
		}

		void ShowImportOperationsManagementWindow(InteractionContext<Unit, Unit> interaction) {
			var assetOperationsWindow = new ImportOperationsManagementWindow {
				ViewModel = new ImportOperationsManagementWindowViewModel(Locator.Current.GetService<StateManager>())
			};
			assetOperationsWindow.Show(this);
			interaction.SetOutput(Unit.Default);
		}
	}
}