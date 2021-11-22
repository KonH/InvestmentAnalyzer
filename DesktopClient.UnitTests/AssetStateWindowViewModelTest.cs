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
		public void IsDistinctPeriodsForAllBrokersShown() {
			var vm = CreateViewModel();

			var periods = vm.StatePeriods.ToArray();
			periods.Should().HaveCount(2);
			periods.Should().Contain(_periods[0]);
			periods.Should().Contain(_periods[1]);
		}

		[Test]
		public void IsAssetsForAllBrokersShown() {
			var vm = CreateViewModel();

			vm.SelectedStatePeriod.Value = _periods[0];

			vm.SelectedPeriodPortfolio.Should().Contain(e => e.BrokerName == _brokers[0]);
			vm.SelectedPeriodPortfolio.Should().Contain(e => e.BrokerName == _brokers[1]);
		}

		AssetStateWindowViewModel CreateViewModel() {
			var repository = new StateRepository();
			var manager = new StateManager(repository);
			var state = manager.State;
			foreach ( var broker in _brokers ) {
				state.Brokers.Add(new BrokerState(broker, string.Empty));
				foreach ( var period in _periods ) {
					if ( !state.Periods.Items.Contains(period) ) {
						state.Periods.Add(period);
					}
					state.Portfolio.Add(new PortfolioState(broker, period, string.Empty));
					state.Entries.Add(new PortfolioStateEntry(
						period, broker, string.Empty, string.Empty, string.Empty, 0, 0, 0));
				}
			}
			return new AssetStateWindowViewModel(manager);
		}
	}
}