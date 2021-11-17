using System.Collections.Generic;

namespace InvestmentAnalyzer.State.Persistant {
	public sealed class AppManifest {
		public Dictionary<string, BrokerManifest> Brokers { get; set; } = new();
	}
}