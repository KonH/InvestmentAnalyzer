using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.ViewModels;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class TagManagementWindow : ReactiveWindow<TagManagementWindowViewModel> {
		public TagManagementWindow() {
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