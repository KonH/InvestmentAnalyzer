using System;
using System.Globalization;
using System.Reactive.Linq;
using InvestmentAnalyzer.DesktopClient.Services;
using Reactive.Bindings;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class GroupStateEntryViewModel {
		public string Group { get; }
		public string Tag { get; }
		public ReactiveProperty<string> Target { get; }

		public ReactiveCommand RemoveEntry { get; }

		public GroupStateEntryViewModel(StateManager manager, string group, string tag, decimal target) {
			Group = group;
			Tag = tag;
			Target = new(target.ToString(CultureInfo.InvariantCulture));
			Target
				.Select(async str => {
					if ( decimal.TryParse(str, out var t) ) {
						await manager.UpdateGroupEntry(group, tag, t);
					}
				})
				.Subscribe();
			RemoveEntry = new();
			RemoveEntry
				.Select(async _ => {
					await manager.RemoveGroupEntry(group, tag);
				})
				.Subscribe();
		}
	}
}