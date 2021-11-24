using System;
using System.Collections.ObjectModel;

namespace InvestmentAnalyzer.State {
	public sealed class CustomLogger {
		public ObservableCollection<string> Lines { get; } = new();

		public void WriteLine(string message) {
			Console.WriteLine(message);
			Lines.Add(message);
		}
	}
}