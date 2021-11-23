using System.Collections.Generic;

namespace InvestmentAnalyzer.State.Persistant {
	public sealed class BrokerManifest {
		public string Name { get; set; } = string.Empty;
		public string StateFormat { get; set; } = string.Empty;
		public string OperationsFormat { get; set; } = string.Empty;
		public Dictionary<string, string> Reports { get; set; } = new();
		public Dictionary<string, string> OperationReports { get; set; } = new();
	}
}