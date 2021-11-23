using System;

namespace InvestmentAnalyzer.State {
	public record PortfolioOperationEntry(
		DateOnly Date, string BrokerName, string Type, string Currency, double Volume);
}