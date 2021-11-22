using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class BrokerManagementWindowViewModel : ViewModelBase {
		public ReactiveProperty<string?> SelectedBroker { get; } = new();

		public ReadOnlyObservableCollection<string> AvailableBrokers => _availableBrokers;

		public ReactiveCommand AddBroker { get; }
		public ReactiveCommand RemoveSelectedBroker { get; }

		public Interaction<Unit, BrokerState?> ShowAddBrokerWindow { get; } = new();

		readonly ReadOnlyObservableCollection<string> _availableBrokers;

		public BrokerManagementWindowViewModel(): this(new StateManager(new StateRepository())) {}

		public BrokerManagementWindowViewModel(StateManager manager) {
			manager.State.Brokers
				.Connect()
				.Transform(b => b.Name)
				.ObserveOnUIDispatcher()
				.Bind(out _availableBrokers)
				.Subscribe();
			AddBroker = new ReactiveCommand();
			AddBroker
				.Select(async _ => {
					var brokerState = await ShowAddBrokerWindow.Handle(Unit.Default);
					if ( brokerState == null ) {
						return;
					}
					await manager.AddBroker(brokerState);
				}).Subscribe();
			RemoveSelectedBroker = new ReactiveCommand(SelectedBroker.Select(b => !string.IsNullOrEmpty(b)));
			RemoveSelectedBroker
				.Select(async _ => {
					var broker = SelectedBroker.Value;
					if ( string.IsNullOrEmpty(broker) ) {
						return;
					}
					await manager.RemoveBroker(broker);
				}).Subscribe();
		}
	}
}