using System;
using System.Reactive;
using System.Reactive.Linq;
using InvestmentAnalyzer.DesktopClient.Services;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class DashboardWindowViewModel : ViewModelBase {
		public ReactiveCommand ShowAssets { get; }
		public ReactiveCommand ShowBrokers { get; }
		public ReactiveCommand ShowImport { get; }

		public Interaction<Unit, Unit> CloseWindow { get; } = new();
		public Interaction<Unit, Unit> ShowAssetStateWindow { get; } = new();
		public Interaction<Unit, Unit> ShowBrokerManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowImportManagementWindow { get; } = new();

		readonly StateManager _manager;

		public DashboardWindowViewModel(): this(new StateManager()) {}

		public DashboardWindowViewModel(StateManager manager) {
			_manager = manager;
			ShowAssets = new ReactiveCommand();
			ShowAssets
				.Select(async _ => await ShowAssetStateWindow.Handle(Unit.Default))
				.Subscribe();
			ShowBrokers = new ReactiveCommand();
			ShowBrokers
				.Select(async _ => await ShowBrokerManagementWindow.Handle(Unit.Default))
				.Subscribe();
			ShowImport = new ReactiveCommand();
			ShowImport
				.Select(async _ => await ShowImportManagementWindow.Handle(Unit.Default))
				.Subscribe();
		}
	}
}