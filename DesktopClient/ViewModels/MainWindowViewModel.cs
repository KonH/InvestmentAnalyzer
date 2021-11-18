using System;
using System.Collections.ObjectModel;
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

		readonly StateManager _manager;

		public MainWindowViewModel() {
			var repository = new StateRepository();
			_manager = new StateManager(repository);
			_manager.State.Brokers
				.ObserveAddChanged()
				.Subscribe(b => {
					SelectedBroker.Value ??= b;
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
			SelectedStatePeriod
				.Subscribe(p => {
					SelectedPeriodPortfolio.ReplaceWithRange(p?.Entries);
				});
		}

		public async Task LoadStartup() =>
			await _manager.LoadStartup();

		public async Task<bool> TryInitialize() =>
			await _manager.TryInitialize();

		public async Task<bool> InitializeWithPath(string path, bool allowCreate) =>
			await _manager.Initialize(path, allowCreate);
	}
}