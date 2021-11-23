using System;
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

		public string[] AvailableOperationFormats => new[] {
			"AlphaDirectMoneyMove",
			"TinkoffMoneyMove",
		};

		public ReactiveProperty<string> Name { get; } = new();
		public ReactiveProperty<string> StateFormat { get; } = new();
		public ReactiveProperty<string> OperationsFormat { get; } = new();

		public Interaction<BrokerState?, Unit> CloseWindow { get; } = new();

		public ReactiveCommand Add { get; }
		public ReactiveCommand Cancel { get; }

		public AddBrokerWindowViewModel() {
			StateFormat.Value = AvailableStateFormats.First();
			OperationsFormat.Value = AvailableOperationFormats.First();
			Add = new ReactiveCommand(
				Name.CombineLatest(StateFormat, OperationsFormat,
					(name, stateFormat, operationsFormat) =>
						!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(stateFormat) && !string.IsNullOrEmpty(operationsFormat)));
			Add
				.Select(async _ => {
					var state = new BrokerState(Name.Value, StateFormat.Value, OperationsFormat.Value);
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