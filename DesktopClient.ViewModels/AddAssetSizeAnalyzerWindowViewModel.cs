using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class AddAssetSizeAnalyzerWindowViewModel : ViewModelBase {
		public ReactiveProperty<string> Size { get; } = new();

		public Interaction<AnalyzerState?, Unit> CloseWindow { get; } = new();

		public ReactiveCommand Add { get; }
		public ReactiveCommand Cancel { get; }

		public AddAssetSizeAnalyzerWindowViewModel() {
			Add = new(Size.Select(size => decimal.TryParse(size, NumberStyles.Any, CultureInfo.InvariantCulture, out _)));
			Add
				.Select(async _ => {
					var state = new AnalyzerState(Guid.NewGuid().ToString(), Analyzers.AssetSize, Size.Value);
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