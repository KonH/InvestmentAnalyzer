using System;
using System.Reactive;
using System.Reactive.Linq;
using InvestmentAnalyzer.DesktopClient.Services;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class DashboardWindowViewModel : ViewModelBase {
		public ReactiveCommand ShowAssets { get; }
		public ReactiveCommand ShowAssetPlot { get; }
		public ReactiveCommand ShowOperations { get; }
		public ReactiveCommand ShowBrokers { get; }
		public ReactiveCommand ShowTags { get; }
		public ReactiveCommand ShowImportState { get; }
		public ReactiveCommand ShowImportOperations { get; }
		public ReactiveCommand ClearLog { get; }

		public ReactiveProperty<string> LogText { get; }

		public Interaction<Unit, Unit> CloseWindow { get; } = new();
		public Interaction<Unit, Unit> ShowAssetStateWindow { get; } = new();
		public Interaction<Unit, Unit> ShowAssetPlotWindow { get; } = new();
		public Interaction<Unit, Unit> ShowOperationsWindow { get; } = new();
		public Interaction<Unit, Unit> ShowBrokerManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowTagManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowImportStateManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowImportOperationsManagementWindow { get; } = new();

		public DashboardWindowViewModel(): this(new StateManager()) {}

		public DashboardWindowViewModel(StateManager manager) {
			ShowAssets = new ReactiveCommand();
			ShowAssets
				.Select(async _ => await ShowAssetStateWindow.Handle(Unit.Default))
				.Subscribe();
			ShowAssetPlot = new ReactiveCommand();
			ShowAssetPlot
				.Select(async _ => await ShowAssetPlotWindow.Handle(Unit.Default))
				.Subscribe();
			ShowOperations = new ReactiveCommand();
			ShowOperations
				.Select(async _ => await ShowOperationsWindow.Handle(Unit.Default))
				.Subscribe();
			ShowBrokers = new ReactiveCommand();
			ShowBrokers
				.Select(async _ => await ShowBrokerManagementWindow.Handle(Unit.Default))
				.Subscribe();
			ShowTags = new ReactiveCommand();
			ShowTags
				.Select(async _ => await ShowTagManagementWindow.Handle(Unit.Default))
				.Subscribe();
			ShowImportState = new ReactiveCommand();
			ShowImportState
				.Select(async _ => await ShowImportStateManagementWindow.Handle(Unit.Default))
				.Subscribe();
			ShowImportOperations = new ReactiveCommand();
			ShowImportOperations
				.Select(async _ => await ShowImportOperationsManagementWindow.Handle(Unit.Default))
				.Subscribe();
			LogText = new ReactiveProperty<string>(string.Join('\n', manager.LogLines));
			ClearLog = new ReactiveCommand();
			ClearLog
				.Select(_ => LogText.Value = string.Empty)
				.Subscribe();
			manager.LogLines
				.ObserveAddChanged()
				.Subscribe(v => {
					LogText.Value += $"\n{v}";
				});
		}
	}
}