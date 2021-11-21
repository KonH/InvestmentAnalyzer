using DynamicData;

namespace InvestmentAnalyzer.State {
	public record AppState(
		SourceList<BrokerState> Brokers,
		SourceList<PortfolioState> Portfolio,
		SourceList<PortfolioStateEntry> Entries);
}