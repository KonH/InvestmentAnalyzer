using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace InvestmentAnalyzer.State {
	public record AppState(ObservableCollection<BrokerState> Brokers);
}