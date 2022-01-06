using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using InvestmentAnalyzer.State;
using ReactiveUI;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class AssetAnalyzeWindow : ReactiveWindow<AssetAnalyzeWindowViewModel> {
		public AssetAnalyzeWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(d => d(ViewModel!.ShowAddAnalyzerWindow.RegisterHandler(ShowAddAnalyzerWindow)));
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		async Task ShowAddAnalyzerWindow(InteractionContext<Unit, AnalyzerState?> interaction) {
			var window = new AddAnalyzerWindow {
				ViewModel = new AddAnalyzerWindowViewModel()
			};
			var analyzer = await window.ShowDialog<AnalyzerState>(this);
			interaction.SetOutput(analyzer);
		}
	}
}