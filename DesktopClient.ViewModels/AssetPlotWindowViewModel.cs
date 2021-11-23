using System.Collections.Generic;
using DesktopClient.Services;
using InvestmentAnalyzer.DesktopClient.Services;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public sealed class AssetPlotWindowViewModel : ViewModelBase {
		public List<AssetPriceMeasurement> Measurements { get; }

		public AssetPlotWindowViewModel(): this(new StateManager()) {}

		public AssetPlotWindowViewModel(StateManager manager) {
			Measurements = new List<AssetPriceMeasurement>(manager.CalculateAssetPriceMeasurements());
		}
	}
}