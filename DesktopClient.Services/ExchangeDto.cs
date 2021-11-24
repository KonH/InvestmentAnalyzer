using System;

namespace InvestmentAnalyzer.State {
	public record ExchangeDto(DateOnly Date, string CharCode, decimal Nominal, decimal Value);
}