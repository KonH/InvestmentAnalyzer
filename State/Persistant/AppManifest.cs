using System.Collections.Generic;

namespace InvestmentAnalyzer.State.Persistant {
	public sealed class AppManifest {
		public List<BrokerManifest> Brokers { get; set; } = new();
	}
}