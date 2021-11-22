using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using InvestmentAnalyzer.State;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class DashboardWindowViewModel : ViewModelBase {
		public ReactiveCommand ShowAssets { get; }
		public ReactiveCommand ShowBrokers { get; }
		public ReactiveCommand ShowImport { get; }

		public Interaction<Unit, bool> ShowChooseStateDialog { get; } = new();
		public Interaction<OpenFileDialogOptions, string[]> ShowOpenFileDialog { get; } = new();
		public Interaction<Unit, string> ShowSaveFileDialog { get; } = new();
		public Interaction<Unit, Unit> CloseWindow { get; } = new();
		public Interaction<Unit, Unit> ShowAssetStateWindow { get; } = new();
		public Interaction<Unit, Unit> ShowBrokerManagementWindow { get; } = new();
		public Interaction<Unit, Unit> ShowImportManagementWindow { get; } = new();

		readonly StateManager _manager;

		public DashboardWindowViewModel(): this(new StateManager(new StateRepository())) {}

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

		public async Task Initialize() {
			await _manager.LoadStartup();
			var isInitializedWithDefaults = await _manager.TryInitialize();
			if ( isInitializedWithDefaults ) {
				return;
			}
			var shouldCreate = await ShowChooseStateDialog.Handle(Unit.Default);
			if ( shouldCreate ) {
				var path = await ShowSaveFileDialog.Handle(Unit.Default);
				var isInitialized = await _manager.Initialize(path, true);
				if ( isInitialized ) {
					return;
				}
			} else {
				var result = await ShowOpenFileDialog.Handle(new OpenFileDialogOptions(
					false, new FileDialogFilter { Extensions = new List<string> { "zip" } }));
				var path = result.First();
				var isInitialized = await _manager.Initialize(path, false);
				if ( isInitialized ) {
					return;
				}
			}
			await CloseWindow.Handle(Unit.Default);
		}
	}
}