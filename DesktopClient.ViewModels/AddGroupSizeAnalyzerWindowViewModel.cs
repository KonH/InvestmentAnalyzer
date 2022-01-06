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
	public sealed class AddGroupSizeAnalyzerWindowViewModel : ViewModelBase {
		public ReactiveProperty<string> GroupName { get; } = new();
		
		public string[] GroupNames { get; }

		public Interaction<AnalyzerState?, Unit> CloseWindow { get; } = new();

		public ReactiveCommand Add { get; }
		public ReactiveCommand Cancel { get; }

		public AddGroupSizeAnalyzerWindowViewModel() : this(new()) {}
		
		public AddGroupSizeAnalyzerWindowViewModel(StateManager manager) {
			GroupNames = manager.State.Groups.Items
				.Select(g => g.Name)
				.ToArray();
			Add = new(GroupName.Select(name => !string.IsNullOrEmpty(name)));
			Add
				.Select(async _ => {
					var state = new AnalyzerState(Guid.NewGuid().ToString(), Analyzers.GroupSize, GroupName.Value);
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