using System;

namespace DesktopClient.Services {
	public record AssetPriceMeasurement(DateTime Date, double TotalPrice, double CumulativeFunds);
}