using System;
using System.Reactive;
using System.Reactive.Linq;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class AddAnalyzerWindowViewModel : ViewModelBase {
		public ReactiveProperty<string> AnalyzerName { get; } = new();

		public string[] AnalyzerNames { get; } = {
			Analyzers.AssetSize,
			Analyzers.GroupSize,
		};

		public Interaction<string?, Unit> CloseWindow { get; } = new();

		public ReactiveCommand Add { get; }
		public ReactiveCommand Cancel { get; }

		public AddAnalyzerWindowViewModel() {
			Add = new(AnalyzerName.Select(name => !string.IsNullOrEmpty(name)));
			Add
				.Select(async _ => {
					var state = AnalyzerName.Value;
					await CloseWindow.Handle(state);
				})
				.Subscribe();
			Cancel = new();
			Cancel
				.Select(async _ => {
					await CloseWindow.Handle(null);
				})
				.Subscribe();
		}
	}
}