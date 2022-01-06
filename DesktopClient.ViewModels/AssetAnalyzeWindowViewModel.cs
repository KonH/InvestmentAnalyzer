using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.State;
using Reactive.Bindings.Extensions;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class AssetAnalyzeWindowViewModel : ViewModelBase {
		public ReadOnlyObservableCollection<PortfolioAnalyzeEntry> LatestPortfolio => _latestPortfolio;
		
		public ReadOnlyObservableCollection<AnalyzerStateViewModel> Analyzers => _analyzers;
		
		public ReactiveCommand AddAnalyzer { get; }

		public Interaction<Unit, AnalyzerState?> ShowAddAnalyzerWindow { get; } = new();

		readonly ReadOnlyObservableCollection<PortfolioAnalyzeEntry> _latestPortfolio;
		readonly ReadOnlyObservableCollection<AnalyzerStateViewModel> _analyzers;

		public AssetAnalyzeWindowViewModel(): this(new()) {}

		public AssetAnalyzeWindowViewModel(StateManager manager) {
			manager.RefreshAnalyze();
			manager.State.Analyzers
				.Connect()
				.ObserveOnUIDispatcher()
				.Transform(e => new AnalyzerStateViewModel(manager, e))
				.Bind(out _analyzers)
				.Subscribe();
			manager.State.AnalyzeEntries
				.Connect()
				.ObserveOnUIDispatcher()
				.Bind(out _latestPortfolio)
				.Subscribe();
			AddAnalyzer = new();
			AddAnalyzer
				.Select(async _ => {
					var analyzerState = await ShowAddAnalyzerWindow.Handle(Unit.Default);
					if ( analyzerState != null ) {
						await manager.AddAnalyzer(analyzerState);
						manager.RefreshAnalyze();
					}
				})
				.Subscribe();
		}
	}
}