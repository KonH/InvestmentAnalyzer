using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using InvestmentAnalyzer.State;
using ReactiveUI;
using Splat;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public class AddAnalyzerWindow : ReactiveWindow<AddAnalyzerWindowViewModel> {
		public AddAnalyzerWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(d => d(ViewModel!.CloseWindow.RegisterHandler(CloseWindow)));
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		async Task CloseWindow(InteractionContext<string?, Unit> interaction) {
			var analyzerType = interaction.Input;
			switch ( analyzerType ) {
				case Analyzers.AssetSize: {
					var window = new AddAssetSizeAnalyzerWindow {
						ViewModel = new()
					};
					var analyzer = await window.ShowDialog<AnalyzerState>(this);
					Close(analyzer);
					return;
				}
				case Analyzers.GroupSize: {
					var window = new AddGroupSizeAnalyzerWindow {
						ViewModel = new(Locator.Current.GetService<StateManager>())
					};
					var analyzer = await window.ShowDialog<AnalyzerState>(this);
					Close(analyzer);
					break;
				}
			}
			Close(null);
		}
	}
}