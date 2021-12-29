using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.State;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ReactiveUI;
using ReactiveCommand = Reactive.Bindings.ReactiveCommand;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class GroupManagementWindowViewModel : ViewModelBase {
		public ReactiveProperty<string?> SelectedGroup { get; } = new();

		public ReadOnlyObservableCollection<string> Groups => _groups;

		public ReadOnlyObservableCollection<GroupStateEntryViewModel> SelectedGroupEntries => _selectedGroupEntries;

		public ReactiveProperty<decimal> SelectedGroupTarget { get; } = new();

		public ReactiveProperty<int> UnusedTagsCount { get; } = new();
		public ReactiveProperty<string> UnusedTagsString { get; } = new();

		public ObservableCollection<GroupStateSummaryViewModel> SelectedGroupPortfolio { get; } = new();

		public ReactiveCommand AddNewGroup { get; }
		public ReactiveCommand RemoveSelectedGroup { get; }

		public ReactiveCommand AddNewEntry { get; }

		public Interaction<Unit, string?> ShowNewGroupDialog { get; } = new();
		public Interaction<Unit, string?> ShowNewEntryDialog { get; } = new();

		readonly StateManager _manager;

		readonly ReadOnlyObservableCollection<string> _groups;

		readonly ReadOnlyObservableCollection<GroupStateEntryViewModel> _selectedGroupEntries;
		readonly ReadOnlyObservableCollection<GroupStateEntry> _selectedGroupRawEntries;

		public GroupManagementWindowViewModel(): this(new()) {}

		public GroupManagementWindowViewModel(StateManager manager) {
			_manager = manager;
			_manager.EnsureAssetTags();
			manager.State.Groups
				.Connect()
				.Transform(b => b.Name)
				.ObserveOnUIDispatcher()
				.Bind(out _groups)
				.Subscribe();
			Func<GroupStateEntry, bool> MakeGroupFilter(string? groupName) =>
				e => e.Group == groupName;
			manager.State.GroupEntries
				.Connect()
				.Filter(SelectedGroup.Select(MakeGroupFilter))
				.Transform(e => new GroupStateEntryViewModel(manager, e.Group, e.Tag, e.Target))
				.ObserveOnUIDispatcher()
				.Bind(out _selectedGroupEntries)
				.Subscribe();
			manager.State.GroupEntries
				.Connect()
				.Filter(SelectedGroup.Select(MakeGroupFilter))
				.ObserveOnUIDispatcher()
				.Bind(out _selectedGroupRawEntries)
				.Subscribe();
			Observable.Interval(TimeSpan.FromSeconds(0.5))
				.ObserveOnUIDispatcher()
				.Do(_ => {
					SelectedGroupTarget.Value = _selectedGroupRawEntries.Sum(e => e.Target);
					UpdateUnusedTags();
					UpdateSelectedGroupPortfolio();
				})
				.Subscribe();
			AddNewGroup = new();
			AddNewGroup
				.Select(async _ => {
					var groupName = await ShowNewGroupDialog.Handle(Unit.Default);
					if ( !string.IsNullOrEmpty(groupName) ) {
						await manager.AddNewGroup(groupName);
					}
				})
				.Subscribe();
			RemoveSelectedGroup = new(SelectedGroup.Select(n => !string.IsNullOrEmpty(n)));
			RemoveSelectedGroup
				.Select(async _ => {
					await manager.RemoveGroup(SelectedGroup.Value!);
				})
				.Subscribe();
			AddNewEntry = new(SelectedGroup.Select(n => !string.IsNullOrEmpty(n)));
			AddNewEntry
				.Select(async _ => {
					var groupName = SelectedGroup.Value!;
					var tag = await ShowNewEntryDialog.Handle(Unit.Default);
					if ( !string.IsNullOrEmpty(tag) ) {
						await manager.AddNewGroupEntry(groupName, tag, 0);
					}
				})
				.Subscribe();
		}

		void UpdateUnusedTags() {
			var usedTags = _manager.State.GroupEntries.Items
				.Select(e => e.Tag)
				.Distinct();
			var unusedTags = _manager.State.Tags.Items.Except(usedTags).ToArray();
			UnusedTagsCount.Value = unusedTags.Length;
			UnusedTagsString.Value = string.Join(", ", unusedTags);
		}

		void UpdateSelectedGroupPortfolio() {
			var brokers = _manager.State.Brokers.Items
				.Select(b => b.Name)
				.ToArray();
			var latestAssets = brokers
				.Select(b => {
					var bp = _manager.State.Portfolio.Items
						.Where(s => s.BrokerName == b)
						.OrderByDescending(s => s.Date)
						.FirstOrDefault();
					if ( bp != null ) {
						return _manager.State.Entries.Items
							.Where(e => e.BrokerName == b && e.Date == bp.Date)
							.ToArray();
					}
					return Array.Empty<PortfolioStateEntry>();
				})
				.SelectMany(e => e)
				.ToArray();
			SelectedGroupPortfolio.Clear();
			var totalPrice = GetNormalizedPrice(latestAssets);
			foreach ( var groupEntry in _selectedGroupRawEntries ) {
				var tag = groupEntry.Tag;
				var assetsForTag = latestAssets
					.Where(e =>
						_manager.State.AssetTags.Items.Any(t => t.Isin == e.Isin && t.Tags.Items.Contains(tag)))
					.ToArray();
				var target = groupEntry.Target;
				var price = GetNormalizedPrice(assetsForTag);
				var actualRatio = Math.Round(totalPrice > 0 ? price / totalPrice * 100 : 0, 2);
				var diff = actualRatio - target;
				SelectedGroupPortfolio.Add(new() {
					Tag = tag,
					Price = price,
					Target = target,
					ActualRatio = actualRatio,
					Diff = diff,
					Assets = assetsForTag.Length
				});
			}
		}

		decimal GetNormalizedPrice(IReadOnlyCollection<PortfolioStateEntry> entries) =>
			entries
				.Sum(e => _manager.GetConvertedPrice(e.Currency, e.TotalPrice, e.Date));
	}
}