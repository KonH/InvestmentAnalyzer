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
		public ReactiveProperty<AssetTagStateViewModel> SelectedAsset { get; } = new();
		public ReactiveProperty<string> NewTag { get; } = new();

		public ReadOnlyObservableCollection<string> Tags => _tags;
		public ReadOnlyObservableCollection<AssetTagStateViewModel> AssetTags => _assetTags;

		public ReactiveCommand AddNewTag { get; }
		public ReactiveCommand RemoveSelectedTag { get; }
		public ReactiveCommand AddSelectedAssetTag { get; }
		public ReactiveCommand RemoveSelectedAssetTag { get; }

		readonly ReadOnlyObservableCollection<string> _tags;
		readonly ReadOnlyObservableCollection<AssetTagStateViewModel> _assetTags;

		public TagManagementWindowViewModel(): this(new StateManager()) {}

		public TagManagementWindowViewModel(StateManager manager) {
			manager.State.Tags
				.Connect()
				.ObserveOnUIDispatcher()
				.Bind(out _tags)
				.Subscribe();
			manager.EnsureAssetTags();
			manager.State.AssetTags
				.Connect()
				.ObserveOnUIDispatcher()
				.Transform(e => new AssetTagStateViewModel(e))
				.Bind(out _assetTags)
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
			AddSelectedAssetTag = new ReactiveCommand(SelectedTag.Select(v => !string.IsNullOrEmpty(v)));
			AddSelectedAssetTag
				.Select(async _ => {
					await manager.AddAssetTag(SelectedAsset.Value.Isin, SelectedTag.Value);
				})
				.Subscribe();
			RemoveSelectedAssetTag = new ReactiveCommand(SelectedTag.Select(v => !string.IsNullOrEmpty(v)));
			RemoveSelectedAssetTag
				.Select(async _ => {
					await manager.RemoveAssetTag(SelectedAsset.Value.Isin, SelectedTag.Value);
				})
				.Subscribe();
		}
	}
}