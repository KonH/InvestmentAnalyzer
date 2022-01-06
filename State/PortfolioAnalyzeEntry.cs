namespace InvestmentAnalyzer.State {
	public record PortfolioAnalyzeEntry(
		string BrokerNames, string Isin, string Name, string Currency, decimal TotalCount, decimal TotalPrice, decimal Score);
}