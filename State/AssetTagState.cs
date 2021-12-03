using DynamicData;

namespace InvestmentAnalyzer.State {
	public record AssetTagState(string Isin, string Name, string Currency, SourceList<string> Tags);
}