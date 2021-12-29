namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class GroupStateSummaryViewModel {
		public string Tag { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public decimal Target { get; set; }
		public decimal ActualRatio { get; set; }
		public decimal Diff { get; set; }
		public int Assets { get; set; }
	}
}