﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InvestmentAnalyzer.DesktopClient.Utils;
using InvestmentAnalyzer.Importer;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class MainWindowViewModel : ViewModelBase {
		public static readonly MainWindowViewModel DebugInstance = new();

		public AppState State => _manager.State;

		public ReactiveProperty<BrokerState?> SelectedBroker { get; } = new();

		public ObservableCollection<PortfolioState> SelectedBrokerStatePeriods { get; } = new();

		public ReactiveProperty<PortfolioState?> SelectedStatePeriod { get; } = new();

		public ObservableCollection<Common.StateEntry> SelectedPeriodPortfolio { get; } = new();

		public ReactiveCommand RemoveSelectedBroker { get; }
		public ReactiveCommand RemoveSelectedState { get; }

		readonly StateManager _manager;

		public MainWindowViewModel() {
			var repository = new StateRepository();
			_manager = new StateManager(repository);
			_manager.State.Brokers
				.ObserveAddChanged()
				.Subscribe(b => {
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
					SelectedBrokerStatePeriods.ReplaceWithRange(b?.Portfolio.Values);
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
			RemoveSelectedBroker = new ReactiveCommand(SelectedBroker.Select(b => b != null));
			RemoveSelectedBroker
				.Select(async _ => {
					var broker = SelectedBroker.Value;
					if ( broker == null ) {
						return;
					}
					await _manager.RemoveBroker(broker.Name);
				}).Subscribe();
			RemoveSelectedState = new ReactiveCommand(SelectedStatePeriod.Select(p => p != null));
			RemoveSelectedState
				.Select(async _ => {
					var broker = SelectedBroker.Value;
					var period = SelectedStatePeriod.Value;
					if ( (broker == null) || (period == null) ) {
						return;
					}
					await _manager.RemovePortfolioPeriod(broker.Name, period.Date);
					SelectedBrokerStatePeriods.Remove(period);
				}).Subscribe();
		}

		public async Task LoadStartup() =>
			await _manager.LoadStartup();

		public async Task<bool> TryInitialize() =>
			await _manager.TryInitialize();

		public async Task<bool> InitializeWithPath(string path, bool allowCreate) =>
			await _manager.Initialize(path, allowCreate);
	}
}