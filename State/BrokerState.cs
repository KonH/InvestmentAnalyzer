using System.Collections.ObjectModel;

namespace InvestmentAnalyzer.State {
	public record BrokerState(string Name, string StateFormat, ObservableCollection<PortfolioState> Portfolio);
}