using System;
using System.Globalization;
using System.Reactive.Linq;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class AnalyzerStateViewModel {
		public string Type { get; }
		public ReactiveProperty<string> Weight { get; }

		public ReactiveCommand RemoveEntry { get; }

		public AnalyzerStateViewModel(StateManager manager, AnalyzerState analyzer) {
			Type = $"{analyzer.Type}: {analyzer.Argument}";
			Weight = new(analyzer.Weight.ToString(CultureInfo.InvariantCulture));
			Weight
				.Select(async str => {
					if ( decimal.TryParse(str, out var t) ) {
						await manager.UpdateAnalyzer(analyzer.Id, t);
						manager.RefreshAnalyze();
					}
				})
				.Subscribe();
			RemoveEntry = new();
			RemoveEntry
				.Select(async _ => {
					await manager.RemoveAnalyzer(analyzer.Id);
					manager.RefreshAnalyze();
				})
				.Subscribe();
		}
	}
}