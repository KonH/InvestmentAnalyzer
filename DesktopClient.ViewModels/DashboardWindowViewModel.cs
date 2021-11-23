using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class DashboardWindowViewModel : ViewModelBase {
		public ReactiveCommand ShowAssets { get; }
		public ReactiveCommand ShowAssetPlot { get; }
		public ReactiveCommand ShowBrokers { get; }
		public ReactiveCommand ShowImportState { get; }
		public ReactiveCommand ShowImportOperations { get; }

		public Interaction<Unit, Unit> CloseWindow { get; } = new();
		public Interaction<Unit, Unit> ShowAssetStateWindow { get; } = new();
		public Interaction<Unit, Unit> ShowAssetPlotWindow { get; } = new();
		public Interaction<Unit, Unit> ShowBrokerManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowImportStateManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowImportOperationsManagementWindow { get; } = new();

		public DashboardWindowViewModel() {
			ShowAssets = new ReactiveCommand();
			ShowAssets
				.Select(async _ => await ShowAssetStateWindow.Handle(Unit.Default))
				.Subscribe();
			ShowAssetPlot = new ReactiveCommand();
			ShowAssetPlot
				.Select(async _ => await ShowAssetPlotWindow.Handle(Unit.Default))
				.Subscribe();
			ShowBrokers = new ReactiveCommand();
			ShowBrokers
				.Select(async _ => await ShowBrokerManagementWindow.Handle(Unit.Default))
				.Subscribe();
			ShowImportState = new ReactiveCommand();
			ShowImportState
				.Select(async _ => await ShowImportStateManagementWindow.Handle(Unit.Default))
				.Subscribe();
			ShowImportOperations = new ReactiveCommand();
			ShowImportOperations
				.Select(async _ => await ShowImportOperationsManagementWindow.Handle(Unit.Default))
				.Subscribe();
		}
	}
}