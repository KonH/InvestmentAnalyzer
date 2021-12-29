using System;
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
		StateManager StateManager => Locator.Current.GetService<StateManager>();

		public DashboardWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(_ => {
				var vm = ViewModel!;
				vm.CloseWindow.RegisterHandler(this.CloseWindow);
				vm.ShowAssetStateWindow.RegisterHandler(
					ShowWindow<AssetStateWindow, AssetStateWindowViewModel>(() => new(StateManager)));
				vm.ShowAssetPlotWindow.RegisterHandler(
					ShowWindow<AssetPlotWindow, AssetPlotWindowViewModel>(() => new(StateManager)));
				vm.ShowOperationsWindow.RegisterHandler(
					ShowWindow<OperationsWindow, OperationsWindowViewModel>(() => new(StateManager)));
				vm.ShowBrokerManagementWindow.RegisterHandler(
					ShowWindow<BrokerManagementWindow, BrokerManagementWindowViewModel>(() => new(StateManager)));
				vm.ShowTagManagementWindow.RegisterHandler(
					ShowWindow<TagManagementWindow, TagManagementWindowViewModel>(() => new(StateManager)));
				vm.ShowGroupManagementWindow.RegisterHandler(
					ShowWindow<GroupManagementWindow, GroupManagementWindowViewModel>(() => new(StateManager)));
				vm.ShowImportStateManagementWindow.RegisterHandler(
					ShowWindow<ImportStateManagementWindow, ImportStateManagementWindowViewModel>(() => new(StateManager)));
				vm.ShowImportOperationsManagementWindow.RegisterHandler(
					ShowWindow<ImportOperationsManagementWindow, ImportOperationsManagementWindowViewModel>(() => new(StateManager)));
			});
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		Action<InteractionContext<Unit, Unit>> ShowWindow<TWindow, TViewModel>(Func<TViewModel> viewModel)
			where TWindow : ReactiveWindow<TViewModel>, new()
			where TViewModel : class =>
			interaction => {
				var window = new TWindow {
					ViewModel = viewModel()
				};
				window.Show(this);
				interaction.SetOutput(Unit.Default);
			};
	}
}