using System;
using System.Collections.ObjectModel;
using DynamicData;
using InvestmentAnalyzer.State;
using Reactive.Bindings.Extensions;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public class AssetTagStateViewModel {
		public string Isin { get; }
		public string Name { get; }
		public string Currency { get; }

		public ReadOnlyObservableCollection<string> Tags => _tags;

		readonly ReadOnlyObservableCollection<string> _tags;

		public AssetTagStateViewModel(AssetTagState state) {
			Isin = state.Isin;
			Name = state.Name;
			Currency = state.Currency;
			state.Tags
				.Connect()
				.ObserveOnUIDispatcher()
				.Bind(out _tags)
				.Subscribe();
		}
	}
}