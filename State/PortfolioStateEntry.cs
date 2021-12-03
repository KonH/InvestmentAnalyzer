using System;

namespace InvestmentAnalyzer.State {
	public record PortfolioStateEntry(
		DateOnly Date, string BrokerName, string Isin, string Name, string Currency, decimal Count, decimal TotalPrice, decimal PricePerUnit);
}