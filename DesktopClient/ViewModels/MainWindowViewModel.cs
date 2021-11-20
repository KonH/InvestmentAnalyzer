using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using InvestmentAnalyzer.DesktopClient.Utils;
using InvestmentAnalyzer.Importer;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class MainWindowViewModel : ViewModelBase {
		public static readonly MainWindowViewModel DebugInstance = new();

		public AppState State => _manager.State;

		public ReactiveProperty<BrokerState?> SelectedBroker { get; } = new();

		public ObservableCollection<PortfolioState> SelectedBrokerStatePeriods { get; } = new();

		public ReactiveProperty<PortfolioState?> SelectedStatePeriod { get; } = new();

		public ObservableCollection<Common.StateEntry> SelectedPeriodPortfolio { get; } = new();

		public Interaction<OpenFileDialogOptions, string[]> ShowOpenFileDialog { get; } = new();
		public Interaction<Unit, string> ShowSaveFileDialog { get; } = new();
		public Interaction<Unit, bool> ShowChooseStateDialog { get; } = new();
		public Interaction<Unit, Unit> CloseWindow { get; } = new();
		public Interaction<Unit, BrokerState?> ShowAddBrokerWindow { get; } = new();

		public ReactiveCommand AddBroker { get; }
		public ReactiveCommand RemoveSelectedBroker { get; }
		public ReactiveCommand ImportState { get; }
		public ReactiveCommand RemoveSelectedState { get; }

		readonly StateManager _manager;

		public MainWindowViewModel() {
			var repository = new StateRepository();
			_manager = new StateManager(repository);
			_manager.State.Brokers
				.ObserveAddChanged()
				.Subscribe(b => {
					b.Portfolio
						.ObserveAddChanged()
						.Where(_ => SelectedBroker.Value == b)
						.Do(p => SelectedBrokerStatePeriods.Add(p))
						.Subscribe();
					b.Portfolio
						.ObserveRemoveChanged()
						.Where(_ => SelectedBroker.Value == b)
						.Do(p => SelectedBrokerStatePeriods.Remove(p))
						.Subscribe();
					SelectedBroker.Value ??= b;
				});
			_manager.State.Brokers
				.ObserveRemoveChanged()
				.Subscribe(b => {
					if ( SelectedBroker.Value == b ) {
						SelectedBroker.Value = _manager.State.Brokers.FirstOrDefault();
					}
				});
			SelectedBroker
				.Subscribe(b => {
					SelectedStatePeriod.Value = null;
					SelectedBrokerStatePeriods.ReplaceWithRange(b?.Portfolio);
				});
			SelectedBrokerStatePeriods
				.ObserveAddChanged()
				.Subscribe(p => {
					SelectedStatePeriod.Value ??= p;
				});
			SelectedBrokerStatePeriods
				.ObserveRemoveChanged()
				.Subscribe(p => {
					if ( SelectedStatePeriod.Value == p ) {
						SelectedStatePeriod.Value = SelectedBrokerStatePeriods.FirstOrDefault();
					}
				});
			SelectedStatePeriod
				.Subscribe(p => {
					SelectedPeriodPortfolio.ReplaceWithRange(p?.Entries);
				});
			AddBroker = new ReactiveCommand();
			AddBroker
				.Select(async _ => {
					var brokerState = await ShowAddBrokerWindow.Handle(Unit.Default);
					if ( brokerState == null ) {
						return;
					}
					await _manager.AddBroker(brokerState);
				}).Subscribe();
			RemoveSelectedBroker = new ReactiveCommand(SelectedBroker.Select(b => b != null));
			RemoveSelectedBroker
				.Select(async _ => {
					var broker = SelectedBroker.Value;
					if ( broker == null ) {
						return;
					}
					await _manager.RemoveBroker(broker.Name);
				}).Subscribe();
			ImportState = new ReactiveCommand(SelectedBroker.Select(b => b != null));
			ImportState
				.Select(async _ => {
					var brokerName = SelectedBroker.Value?.Name ?? string.Empty;
					var paths = await ShowOpenFileDialog.Handle(new OpenFileDialogOptions(true));
					await _manager.ImportPortfolioPeriods(brokerName, paths);
				})
				.Subscribe();
			RemoveSelectedState = new ReactiveCommand(SelectedStatePeriod.Select(p => p != null));
			RemoveSelectedState
				.Select(async _ => {
					var broker = SelectedBroker.Value;
					var period = SelectedStatePeriod.Value;
					if ( (broker == null) || (period == null) ) {
						return;
					}
					await _manager.RemovePortfolioPeriod(broker.Name, period);
					SelectedBrokerStatePeriods.Remove(period);
				}).Subscribe();
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