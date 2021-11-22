using System;
using System.Linq;
using DynamicData;
using FluentAssertions;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using InvestmentAnalyzer.State;
using NUnit.Framework;

namespace DesktopClient.UnitTests {
	public class AssetStateWindowViewModelTest {
		readonly string[] _brokers = { "Broker1", "Broker2" };
		readonly DateOnly[] _periods = { new(2000, 01, 01), new(2000, 02, 01) };

		[Test]
		public void IsPeriodsForGivenBrokerShown() {
			var vm = CreateViewModel();

			vm.SelectedBroker.Value = _brokers[0];

			var periods = vm.SelectedBrokerStatePeriods.ToArray();
			periods.Should().Contain(_periods);
		}

		[Test]
		public void IsAssetsForGivenBrokerAndPeriodShown() {
			var vm = CreateViewModel();

			vm.SelectedBroker.Value = _brokers[0];
			vm.SelectedStatePeriod.Value = _periods[0];

			vm.SelectedPeriodPortfolio.Should().Contain(e => e.BrokerName == _brokers[0]);
			vm.SelectedPeriodPortfolio.Should().NotContain(e => e.BrokerName == _brokers[1]);
		}

		[Test]
		public void IsDistinctPeriodsForAllBrokersShown() {
			var vm = CreateViewModel();

			vm.SelectedBroker.Value = _brokers[0];

			var periods = vm.SelectedBrokerStatePeriods.ToArray();
			periods.Should().ContainSingle(p => _periods.Contains(p));
		}

		[Test]
		public void IsAssetsForAllBrokersShown() {
			var vm = CreateViewModel();

			vm.SelectedBroker.Value = StateManager.AggregationBrokerMarker;
			vm.SelectedStatePeriod.Value = _periods[0];

			vm.SelectedPeriodPortfolio.Should().Contain(e => e.BrokerName == _brokers[0]);
			vm.SelectedPeriodPortfolio.Should().Contain(e => e.BrokerName == _brokers[1]);
		}

		AssetStateWindowViewModel CreateViewModel() {
			var repository = new StateRepository();
			var manager = new StateManager(repository);
			var state = manager.State;
			state.Brokers.Add(new BrokerState(StateManager.AggregationBrokerMarker, string.Empty));
			foreach ( var broker in _brokers ) {
				state.Brokers.Add(new BrokerState(broker, string.Empty));
				foreach ( var period in _periods ) {
					state.Portfolio.Add(new PortfolioState(broker, period, string.Empty));
					state.Entries.Add(new PortfolioStateEntry(
						period, broker, string.Empty, string.Empty, string.Empty, 0, 0, 0));
				}
			}
			return new AssetStateWindowViewModel(manager);
		}
	}
}