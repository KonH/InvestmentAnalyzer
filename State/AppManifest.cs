using System.Collections.Generic;

namespace InvestmentAnalyzer.State {
	public sealed class AppManifest {
		public Dictionary<string, BrokerManifest> Brokers { get; set; } = new();
	}
}