using System.Collections.Generic;

namespace InvestmentAnalyzer.State.Persistant {
	public sealed class AppManifest {
		public List<BrokerManifest> Brokers { get; set; } = new();
		public List<Exchange> Exchanges { get; set; } = new();
		public List<string> Tags { get; set; } = new();
	}
}