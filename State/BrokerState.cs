using System;
using System.Collections.Generic;

namespace InvestmentAnalyzer.State {
	public record BrokerState(string Name, string StateFormat, Dictionary<DateOnly, PortfolioState> Portfolio);
}