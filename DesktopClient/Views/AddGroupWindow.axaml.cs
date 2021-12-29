using System.Reactive;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using InvestmentAnalyzer.State;
using ReactiveUI;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public class AddGroupWindow : ReactiveWindow<AddGroupWindowViewModel> {
		public AddGroupWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(d => d(ViewModel!.CloseWindow.RegisterHandler(CloseWindow)));
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		void CloseWindow(InteractionContext<string?, Unit> interaction) {
			var state = interaction.Input;
			Close(state);
		}
	}
}