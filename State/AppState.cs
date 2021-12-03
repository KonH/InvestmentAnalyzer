using System;
using DynamicData;

namespace InvestmentAnalyzer.State {
	public record AppState(
		SourceList<BrokerState> Brokers,
		SourceList<DateOnly> Periods,
		SourceList<PortfolioState> Portfolio,
		SourceList<OperationState> OperationStates,
		SourceList<PortfolioStateEntry> Entries,
		SourceList<PortfolioOperationEntry> Operations,
		SourceList<string> Tags,
		SourceList<AssetTagState> AssetTags);
}