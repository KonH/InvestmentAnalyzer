namespace InvestmentAnalyzer.State {
	public sealed class AnalyzerState {
		public string Id { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public string Argument { get; set; } = string.Empty;
		public decimal Weight { get; set; } = 1.0m;

		public AnalyzerState() {}
		
		public AnalyzerState(string id, string type, string argument) {
			Id = id;
			Type = type;
			Argument = argument;
		}
	}
}