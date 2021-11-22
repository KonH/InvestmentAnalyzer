using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class ImportManagementWindowViewModel : ViewModelBase {
		public ReactiveProperty<string?> SelectedBroker { get; } = new();

		public ReactiveProperty<DateOnly?> SelectedStatePeriod { get; } = new();

		public ReadOnlyObservableCollection<string> AvailableBrokers => _availableBrokers;

		public ReadOnlyObservableCollection<DateOnly> SelectedBrokerStatePeriods => _selectedBrokerStatePeriods;

		public ReactiveCommand ImportState { get; }
		public ReactiveCommand RemoveSelectedState { get; }

		public Interaction<OpenFileDialogOptions, string[]> ShowOpenFileDialog { get; } = new();

		readonly ReadOnlyObservableCollection<string> _availableBrokers;
		readonly ReadOnlyObservableCollection<DateOnly> _selectedBrokerStatePeriods;

		public ImportManagementWindowViewModel(): this(new StateManager(new StateRepository())) {}

		public ImportManagementWindowViewModel(StateManager manager) {
			manager.State.Brokers
				.Connect()
				.Transform(b => b.Name)
				.Bind(out _availableBrokers)
				.Subscribe();
			Func<PortfolioState, bool> MakeBrokerNameFilterForState(string? brokerName) =>
				p => (p.BrokerName == brokerName);
			manager.State.Portfolio
				.Connect()
				.Filter(SelectedBroker.Select(MakeBrokerNameFilterForState))
				.Transform(p => p.Date)
				.Sort(SortExpressionComparer<DateOnly>.Ascending(p => p))
				.Bind(out _selectedBrokerStatePeriods)
				.Subscribe();
			ImportState = new ReactiveCommand(SelectedBroker.Select(b => !string.IsNullOrEmpty(b)));
			ImportState
				.Select(async _ => {
					var brokerName = SelectedBroker.Value ?? string.Empty;
					var paths = await ShowOpenFileDialog.Handle(new OpenFileDialogOptions(true));
					await manager.ImportPortfolioPeriods(brokerName, paths);
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
					await manager.RemovePortfolioPeriod(broker, period.Value);
				}).Subscribe();
		}
	}
}