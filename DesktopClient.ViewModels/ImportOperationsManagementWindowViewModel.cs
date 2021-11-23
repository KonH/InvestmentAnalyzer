using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class ImportOperationsManagementWindowViewModel : ViewModelBase {
		public ReactiveProperty<string?> SelectedBroker { get; } = new();

		public ReactiveProperty<DateOnly?> SelectedOperationPeriod { get; } = new();

		public ReadOnlyObservableCollection<string> AvailableBrokers => _availableBrokers;

		public ReadOnlyObservableCollection<DateOnly> SelectedBrokerOperationPeriods => _selectedBrokerOperationPeriods;

		public ReactiveCommand ImportOperations { get; }
		public ReactiveCommand RemoveSelectedOperations { get; }

		public Interaction<OpenFileDialogOptions, string[]> ShowOpenFileDialog { get; } = new();

		readonly ReadOnlyObservableCollection<string> _availableBrokers;
		readonly ReadOnlyObservableCollection<DateOnly> _selectedBrokerOperationPeriods;

		public ImportOperationsManagementWindowViewModel(): this(new StateManager()) {}

		public ImportOperationsManagementWindowViewModel(StateManager manager) {
			manager.State.Brokers
				.Connect()
				.Transform(b => b.Name)
				.ObserveOnUIDispatcher()
				.Bind(out _availableBrokers)
				.Subscribe();
			Func<OperationState, bool> MakeBrokerNameFilterForState(string? brokerName) =>
				p => (p.BrokerName == brokerName);
			manager.State.OperationStates
				.Connect()
				.Filter(SelectedBroker.Select(MakeBrokerNameFilterForState))
				.Transform(p => p.Date)
				.Sort(SortExpressionComparer<DateOnly>.Ascending(p => p))
				.ObserveOnUIDispatcher()
				.Bind(out _selectedBrokerOperationPeriods)
				.Subscribe();
			ImportOperations = new ReactiveCommand(SelectedBroker.Select(b => !string.IsNullOrEmpty(b)));
			ImportOperations
				.Select(async _ => {
					var brokerName = SelectedBroker.Value ?? string.Empty;
					var paths = await ShowOpenFileDialog.Handle(new OpenFileDialogOptions(true));
					await manager.ImportOperationPeriods(brokerName, paths);
				})
				.Subscribe();
			RemoveSelectedOperations = new ReactiveCommand(SelectedOperationPeriod.Select(p => p != null));
			RemoveSelectedOperations
				.Select(async _ => {
					var broker = SelectedBroker.Value;
					var period = SelectedOperationPeriod.Value;
					if ( string.IsNullOrEmpty(broker) || (period == null) ) {
						return;
					}
					await manager.RemoveOperationPeriod(broker, period.Value);
				}).Subscribe();
		}
	}
}