using System;

namespace InvestmentAnalyzer.State {
	public record OperationState(string BrokerName, DateOnly Date, string ReportName);
}