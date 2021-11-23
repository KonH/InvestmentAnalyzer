using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class OperationsWindowViewModel : ViewModelBase {
		public ReadOnlyObservableCollection<DateOnly> OperationPeriods => _operationPeriods;

		public ReadOnlyObservableCollection<PortfolioOperationEntry> SelectedPeriodOperations => _selectedPeriodOperations;

		public ReactiveProperty<DateOnly?> SelectedOperationPeriod { get; } = new();

		readonly ReadOnlyObservableCollection<DateOnly> _operationPeriods;
		readonly ReadOnlyObservableCollection<PortfolioOperationEntry> _selectedPeriodOperations;

		public OperationsWindowViewModel(): this(new StateManager()) {}

		public OperationsWindowViewModel(StateManager manager) {
			var lastPeriod = manager.State.Periods.Items.LastOrDefault();
			if ( lastPeriod != default ) {
				SelectedOperationPeriod.Value = lastPeriod;
			}
			manager.State.Periods
				.Connect()
				.Sort(SortExpressionComparer<DateOnly>.Ascending(p => p))
				.ObserveOnUIDispatcher()
				.Bind(out _operationPeriods)
				.Subscribe();
			Func<PortfolioOperationEntry, bool> MakeStatePeriodFilter(DateOnly? date) =>
				e => e.Date == date;
			manager.State.Operations
				.Connect()
				.Filter(SelectedOperationPeriod.Select(MakeStatePeriodFilter))
				.ObserveOnUIDispatcher()
				.Bind(out _selectedPeriodOperations)
				.Subscribe();
		}
	}
}