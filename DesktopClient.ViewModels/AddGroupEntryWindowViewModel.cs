using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class AddGroupEntryWindowViewModel : ViewModelBase {
		public ReactiveProperty<string> Tag { get; } = new();

		public string[] Tags { get; }

		public Interaction<string?, Unit> CloseWindow { get; } = new();

		public ReactiveCommand Add { get; }
		public ReactiveCommand Cancel { get; }

		public AddGroupEntryWindowViewModel() : this(new()) {}

		public AddGroupEntryWindowViewModel(StateManager stateManager) {
			Tags = stateManager.State.Tags.Items.ToArray();
			Add = new(Tag.Select(name => !string.IsNullOrEmpty(name)));
			Add
				.Select(async _ => {
					await CloseWindow.Handle(Tag.Value);
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