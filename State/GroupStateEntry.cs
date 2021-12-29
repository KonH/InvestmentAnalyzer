namespace InvestmentAnalyzer.State {
	public class GroupStateEntry {
		public string Group { get; }
		public string Tag { get; }
		public decimal Target { get; set; }

		public GroupStateEntry(string group, string tag, decimal target) {
			Group = group;
			Tag = tag;
			Target = target;
		}
	}
}