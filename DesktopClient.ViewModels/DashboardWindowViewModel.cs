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
		public ReactiveCommand ShowGroups { get; }
		public ReactiveCommand ShowImportState { get; }
		public ReactiveCommand ShowImportOperations { get; }
		public ReactiveCommand ShowAssetAnalyze { get; }
		public ReactiveCommand ClearLog { get; }

		public ReactiveProperty<string> LogText { get; }

		public Interaction<Unit, Unit> CloseWindow { get; } = new();
		public Interaction<Unit, Unit> ShowAssetStateWindow { get; } = new();
		public Interaction<Unit, Unit> ShowAssetPlotWindow { get; } = new();
		public Interaction<Unit, Unit> ShowOperationsWindow { get; } = new();
		public Interaction<Unit, Unit> ShowBrokerManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowTagManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowGroupManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowImportStateManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowImportOperationsManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowAssetAnalyzeWindow { get; } = new();

		public DashboardWindowViewModel(): this(new StateManager()) {}

		public DashboardWindowViewModel(StateManager manager) {
			ShowAssets = BindToWindow(ShowAssetStateWindow);
			ShowAssetPlot = BindToWindow(ShowAssetPlotWindow);
			ShowOperations = BindToWindow(ShowOperationsWindow);
			ShowBrokers = BindToWindow(ShowBrokerManagementWindow);
			ShowTags = BindToWindow(ShowTagManagementWindow);
			ShowGroups = BindToWindow(ShowGroupManagementWindow);
			ShowImportState = BindToWindow(ShowImportStateManagementWindow);
			ShowImportOperations = BindToWindow(ShowImportOperationsManagementWindow);
			ShowAssetAnalyze = BindToWindow(ShowAssetAnalyzeWindow);
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

		ReactiveCommand BindToWindow(Interaction<Unit, Unit> windowInteraction) {
			var command = new ReactiveCommand();
			command
				.Select(async _ => await windowInteraction.Handle(Unit.Default))
				.Subscribe();
			return command;
		}
	}
}