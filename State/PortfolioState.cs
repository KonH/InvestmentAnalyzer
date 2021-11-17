using System;
using System.Collections.Generic;
using InvestmentAnalyzer.Importer;

namespace InvestmentAnalyzer.State {
	public record PortfolioState(DateOnly Date, string ReportName, List<Common.StateEntry> Entries);
}