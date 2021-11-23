using System;

namespace InvestmentAnalyzer.State {
	public record ExchangeDto(DateOnly Date, string CharCode, double Nominal, double Value);
}