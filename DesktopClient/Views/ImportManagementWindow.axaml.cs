using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using ReactiveUI;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class ImportManagementWindow : ReactiveWindow<ImportManagementWindowViewModel> {
		public ImportManagementWindow() {
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