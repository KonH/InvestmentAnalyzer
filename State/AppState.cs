using System;
using DynamicData;

namespace InvestmentAnalyzer.State {
	public class AppState {
		public SourceList<BrokerState> Brokers { get; } = new();
		public SourceList<DateOnly> Periods { get; } = new();
		public SourceList<PortfolioState> Portfolio { get; } = new();
		public SourceList<OperationState> OperationStates { get; } = new();
		public SourceList<PortfolioStateEntry> Entries { get; } = new();
		public SourceList<PortfolioOperationEntry> Operations { get; } = new();
		public SourceList<string> Tags { get; } = new();
		public SourceList<AssetTagState> AssetTags { get; } = new();
		public SourceList<GroupState> Groups { get; } = new();
		public SourceList<GroupStateEntry> GroupEntries { get; } = new();
	}
}