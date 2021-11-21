using System;

namespace InvestmentAnalyzer.State {
	public record PortfolioState(string BrokerName, DateOnly Date, string ReportName);
}