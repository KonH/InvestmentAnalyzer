using System.Reactive;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using InvestmentAnalyzer.DesktopClient.Views;
using ReactiveUI;

namespace DesktopClient.Views {
	public class InitializationWindow : ReactiveWindow<InitializationWindowViewModel> {
		public InitializationWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(_ => {
				ViewModel?.ShowOpenFileDialog.RegisterHandler(this.ShowOpenFileDialog);
				ViewModel?.ShowSaveFileDialog.RegisterHandler(this.ShowSaveFileDialog);
				ViewModel?.ShowDashboardWindow.RegisterHandler(ShowDashboardWindow);
				ViewModel?.StartInitialize();
			});
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		void ShowDashboardWindow(InteractionContext<Unit, Unit> interaction) {
			if ( Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ) {
				var dashboardWindow = new DashboardWindow {
					ViewModel = new DashboardWindowViewModel(),
				};
				dashboardWindow.Show(this);
			}
			interaction.SetOutput(Unit.Default);
		}
	}
}