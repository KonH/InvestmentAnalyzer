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
	public sealed class AssetStateWindowViewModel : ViewModelBase {
		public ReadOnlyObservableCollection<DateOnly> StatePeriods => _statePeriods;

		public ReadOnlyObservableCollection<PortfolioStateEntry> SelectedPeriodPortfolio => _selectedPeriodPortfolio;

		public ReactiveProperty<DateOnly?> SelectedStatePeriod { get; } = new();

		readonly ReadOnlyObservableCollection<DateOnly> _statePeriods;
		readonly ReadOnlyObservableCollection<PortfolioStateEntry> _selectedPeriodPortfolio;

		public AssetStateWindowViewModel(): this(new StateManager()) {}

		public AssetStateWindowViewModel(StateManager manager) {
			var lastPeriod = manager.State.Periods.Items.LastOrDefault();
			if ( lastPeriod != default ) {
				SelectedStatePeriod.Value = lastPeriod;
			}
			manager.State.Periods
				.Connect()
				.Sort(SortExpressionComparer<DateOnly>.Ascending(p => p))
				.ObserveOnUIDispatcher()
				.Bind(out _statePeriods)
				.Subscribe();
			Func<PortfolioStateEntry, bool> MakeStatePeriodFilter(DateOnly? date) =>
				e => e.Date == date;
			manager.State.Entries
				.Connect()
				.Filter(SelectedStatePeriod.Select(MakeStatePeriodFilter))
				.ObserveOnUIDispatcher()
				.Bind(out _selectedPeriodPortfolio)
				.Subscribe();
		}
	}
}