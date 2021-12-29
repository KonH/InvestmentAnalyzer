using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class AddGroupWindowViewModel : ViewModelBase {
		public ReactiveProperty<string> Name { get; } = new();

		public Interaction<string?, Unit> CloseWindow { get; } = new();

		public ReactiveCommand Add { get; }
		public ReactiveCommand Cancel { get; }

		public AddGroupWindowViewModel() {
			Add = new(Name.Select(name => !string.IsNullOrEmpty(name)));
			Add
				.Select(async _ => {
					await CloseWindow.Handle(Name.Value);
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