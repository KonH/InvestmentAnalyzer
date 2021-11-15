using System.Collections.Generic;

namespace InvestmentAnalyzer.State {
	public sealed class BrokerManifest {
		public Dictionary<string, string> Reports { get; set; } = new();
	}
}