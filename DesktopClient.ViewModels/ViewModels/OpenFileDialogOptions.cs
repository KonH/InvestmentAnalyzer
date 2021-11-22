using Avalonia.Controls;

namespace InvestmentAnalyzer.DesktopClient.ViewModels {
	public record OpenFileDialogOptions(bool AllowMultiple, FileDialogFilter? Filter = null);
}