using System;
using DynamicData;

namespace InvestmentAnalyzer.State {
	public record AppState(
		SourceList<BrokerState> Brokers,
		SourceList<DateOnly> Periods,
		SourceList<PortfolioState> Portfolio,
		SourceList<PortfolioStateEntry> Entries);
}