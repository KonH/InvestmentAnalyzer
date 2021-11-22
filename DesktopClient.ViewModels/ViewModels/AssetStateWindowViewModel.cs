using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Binding;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class AssetStateWindowViewModel : ViewModelBase {
		public ReactiveProperty<string?> SelectedBroker { get; } = new();

		public ReadOnlyObservableCollection<string> AvailableBrokers => _availableBrokers;

		public ReadOnlyObservableCollection<DateOnly> SelectedBrokerStatePeriods => _selectedBrokerStatePeriods;

		public ReadOnlyObservableCollection<PortfolioStateEntry> SelectedPeriodPortfolio => _selectedPeriodPortfolio;

		public ReactiveProperty<DateOnly?> SelectedStatePeriod { get; } = new();

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

		readonly ReadOnlyObservableCollection<string> _availableBrokers;
		readonly ReadOnlyObservableCollection<DateOnly> _selectedBrokerStatePeriods;
		readonly ReadOnlyObservableCollection<PortfolioStateEntry> _selectedPeriodPortfolio;

		public AssetStateWindowViewModel(StateManager manager) {
			_manager = manager;
			_manager.State.Brokers
				.Connect()
				.Transform(b => b.Name)
				.Bind(out _availableBrokers)
				.Subscribe();
			Func<PortfolioState, bool> MakeBrokerNameFilterForState(string? brokerName) =>
				p => (p.BrokerName == brokerName) || (brokerName == StateManager.AggregationBrokerMarker);
			_manager.State.Portfolio
				.Connect()
				.Filter(SelectedBroker.Select(MakeBrokerNameFilterForState))
				.Transform(p => p.Date)
				.Sort(SortExpressionComparer<DateOnly>.Ascending(p => p))
				.Bind(out _selectedBrokerStatePeriods)
				.Subscribe();
			Func<PortfolioStateEntry, bool> MakeBrokerNameFilterForEntry(string? brokerName) =>
				e => (e.BrokerName == brokerName) || (brokerName == StateManager.AggregationBrokerMarker);
			Func<PortfolioStateEntry, bool> MakeStatePeriodFilter(DateOnly? date) =>
				e => e.Date == date;
			_manager.State.Entries
				.Connect()
				.Filter(SelectedStatePeriod.Select(MakeStatePeriodFilter))
				.Filter(SelectedBroker.Select(MakeBrokerNameFilterForEntry))
				.Bind(out _selectedPeriodPortfolio)
				.Subscribe();
			AddBroker = new ReactiveCommand();
			AddBroker
				.Select(async _ => {
					var brokerState = await ShowAddBrokerWindow.Handle(Unit.Default);
					if ( brokerState == null ) {
						return;
					}
					await _manager.AddBroker(brokerState);
				}).Subscribe();
			RemoveSelectedBroker = new ReactiveCommand(SelectedBroker.Select(b => !string.IsNullOrEmpty(b)));
			RemoveSelectedBroker
				.Select(async _ => {
					var broker = SelectedBroker.Value;
					if ( string.IsNullOrEmpty(broker) ) {
						return;
					}
					await _manager.RemoveBroker(broker);
				}).Subscribe();
			ImportState = new ReactiveCommand(SelectedBroker.Select(b => !string.IsNullOrEmpty(b)));
			ImportState
				.Select(async _ => {
					var brokerName = SelectedBroker.Value ?? string.Empty;
					var paths = await ShowOpenFileDialog.Handle(new OpenFileDialogOptions(true));
					await _manager.ImportPortfolioPeriods(brokerName, paths);
				})
				.Subscribe();
			RemoveSelectedState = new ReactiveCommand(SelectedStatePeriod.Select(p => p != null));
			RemoveSelectedState
				.Select(async _ => {
					var broker = SelectedBroker.Value;
					var period = SelectedStatePeriod.Value;
					if ( string.IsNullOrEmpty(broker) || (period == null) ) {
						return;
					}
					await _manager.RemovePortfolioPeriod(broker, period.Value);
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