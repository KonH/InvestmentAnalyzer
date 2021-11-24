using System;

namespace DesktopClient.Services {
	public record AssetPriceMeasurement(DateTime Date, decimal TotalPrice, decimal CumulativeFunds);
}