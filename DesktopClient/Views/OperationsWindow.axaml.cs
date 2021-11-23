using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.ViewModels;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class OperationsWindow : ReactiveWindow<OperationsWindowViewModel> {
		public OperationsWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}