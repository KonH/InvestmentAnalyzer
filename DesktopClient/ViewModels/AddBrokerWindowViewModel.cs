using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class AddBrokerWindowViewModel : ViewModelBase {
		public string[] AvailableStateFormats => new[] {
			"AlphaDirectMyPortfolio",
			"TinkoffMyAssets",
		};

		public ReactiveProperty<string> Name { get; } = new();
		public ReactiveProperty<string> StateFormat { get; } = new();

		public Interaction<BrokerState?, Unit> CloseWindow { get; } = new();

		public ReactiveCommand Add { get; }
		public ReactiveCommand Cancel { get; }

		public AddBrokerWindowViewModel() {
			StateFormat.Value = AvailableStateFormats.First();
			Add = new ReactiveCommand(
				Name.CombineLatest(StateFormat,
					(name, format) => !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(format)));
			Add
				.Select(async _ => {
					var state = new BrokerState(Name.Value, StateFormat.Value, new ObservableCollection<PortfolioState>());
					await CloseWindow.Handle(state);
				})
				.Subscribe();
			Cancel = new ReactiveCommand();
			Cancel
				.Select(async _ => {
					await CloseWindow.Handle(null);
				})
				.Subscribe();
		}
	}
}