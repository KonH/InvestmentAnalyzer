using System;

namespace InvestmentAnalyzer.State {
	public record PortfolioStateEntry(
		DateOnly Date, string BrokerName, string ISIN, string Name, string Currency, double Count, double TotalPrice, double PricePerUnit);
}