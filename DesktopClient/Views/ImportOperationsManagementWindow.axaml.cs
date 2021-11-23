using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using ReactiveUI;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class ImportOperationsManagementWindow : ReactiveWindow<ImportOperationsManagementWindowViewModel> {
		public ImportOperationsManagementWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(d => {
				ViewModel?.ShowOpenFileDialog.RegisterHandler(this.ShowOpenFileDialog);
			});
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}