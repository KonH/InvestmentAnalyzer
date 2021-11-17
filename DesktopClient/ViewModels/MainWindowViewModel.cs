using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData;
using InvestmentAnalyzer.Importer;
using InvestmentAnalyzer.State;
using ReactiveUI;

namespace DesktopClient.ViewModels {
	public class MainWindowViewModel : ViewModelBase {
		public AppState State => _manager.State;

		public string SelectedPath { get; set; } = string.Empty;

		public BrokerState? SelectedBroker {
			get => _selectedBroker;
			set {
				_selectedBroker = value;
				SelectedBrokerStatePeriods.Clear();
				if ( _selectedBroker != null ) {
					SelectedBrokerStatePeriods.AddRange(_selectedBroker.Portfolio.Values);
				}
			}
		}

		public ObservableCollection<PortfolioState> SelectedBrokerStatePeriods { get; } = new();

		public PortfolioState? SelectedStatePeriod {
			get => _selectedStatePeriod;
			set {
				_selectedStatePeriod = value;
				SelectedPeriodPortfolio.Clear();
				if ( _selectedStatePeriod != null ) {
					SelectedPeriodPortfolio.AddRange(_selectedStatePeriod.Entries);
				}
			}
		}

		public ObservableCollection<Common.StateEntry> SelectedPeriodPortfolio { get; } = new();

		readonly StateManager _manager;

		BrokerState? _selectedBroker;

		PortfolioState? _selectedStatePeriod;

		public MainWindowViewModel() {
			var repository = new StateRepository();
			_manager = new StateManager(repository);
		}

		public async Task Initialize() {
			await _manager.Initialize(SelectedPath);
		}
	}
}