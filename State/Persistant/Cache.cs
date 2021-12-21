using System.Collections.Generic;
using InvestmentAnalyzer.Importer;

namespace State.Persistant {
	public sealed class Cache {
		public Dictionary<string, StateImporter.ImportResult> States { get; set; } = new();
		public Dictionary<string, OperationImporter.ImportResult> Operations { get; set; } = new();
	}
}