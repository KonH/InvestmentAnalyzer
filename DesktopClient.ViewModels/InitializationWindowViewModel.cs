using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Avalonia.Controls;
using InvestmentAnalyzer.DesktopClient.Services;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public class InitializationWindowViewModel : ViewModelBase {
		public ReactiveCommand OpenState { get; }
		public ReactiveCommand CreateState { get; }

		public Interaction<OpenFileDialogOptions, string[]> ShowOpenFileDialog { get; } = new();
		public Interaction<Unit, string> ShowSaveFileDialog { get; } = new();
		public Interaction<Unit, Unit> ShowDashboardWindow { get; } = new();

		readonly StateManager _manager;

		readonly ReactiveProperty<bool> _shouldHandleState = new();

		public InitializationWindowViewModel(): this(new StateManager()) {}

		public InitializationWindowViewModel(StateManager manager) {
			_manager = manager;
			OpenState = new ReactiveCommand(_shouldHandleState.Select(v => v));
			OpenState
				.Select(async _ => {
					var result = await ShowOpenFileDialog.Handle(new OpenFileDialogOptions(
						false, new FileDialogFilter { Name = "Filter", Extensions = new List<string> { "zip" } }));
					var path = result.First();
					var isInitialized = await _manager.Initialize(path, false);
					if ( isInitialized ) {
						await ShowDashboardWindow.Handle(Unit.Default);
					}
				})
				.Subscribe();
			CreateState = new ReactiveCommand(_shouldHandleState.Select(v => v));
			CreateState
				.Select(async _ => {
					var path = await ShowSaveFileDialog.Handle(Unit.Default);
					var isInitialized = await _manager.Initialize(path, true);
					if ( isInitialized ) {
						await ShowDashboardWindow.Handle(Unit.Default);
					}
				})
				.Subscribe();
		}

		public void StartInitialize() {
			RxApp.MainThreadScheduler.Schedule(Initialize);
		}

		async void Initialize() {
			await _manager.LoadStartup();
			var isInitializedWithDefaults = await _manager.TryInitialize();
			if ( isInitializedWithDefaults ) {
				await ShowDashboardWindow.Handle(Unit.Default);
			}
			_shouldHandleState.Value = true;
		}
	}
}