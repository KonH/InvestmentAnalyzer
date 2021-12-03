using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using InvestmentAnalyzer.DesktopClient.Services;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class TagManagementWindowViewModel : ViewModelBase {
		public ReactiveProperty<string> SelectedTag { get; } = new();
		public ReactiveProperty<string> NewTag { get; } = new();

		public ReadOnlyObservableCollection<string> Tags => _tags;

		public ReactiveCommand AddNewTag { get; }
		public ReactiveCommand RemoveSelectedTag { get; }

		readonly ReadOnlyObservableCollection<string> _tags;

		public TagManagementWindowViewModel(): this(new StateManager()) {}

		public TagManagementWindowViewModel(StateManager manager) {
			manager.State.Tags
				.Connect()
				.ObserveOnUIDispatcher()
				.Bind(out _tags)
				.Subscribe();
			AddNewTag = new ReactiveCommand(NewTag.Select(v => !string.IsNullOrEmpty(v)));
			AddNewTag
				.Select(async _ => {
					await manager.AddTag(NewTag.Value);
					NewTag.Value = string.Empty;
				})
				.Subscribe();
			RemoveSelectedTag = new ReactiveCommand(SelectedTag.Select(v => !string.IsNullOrEmpty(v)));
			RemoveSelectedTag
				.Select(async _ => {
					await manager.RemoveTag(SelectedTag.Value);
					SelectedTag.Value = string.Empty;
				})
				.Subscribe();
		}
	}
}