using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using InvestmentAnalyzer.DesktopClient.Views;
using InvestmentAnalyzer.State;
using Splat;

namespace InvestmentAnalyzer.DesktopClient {
	public class App : Application {
		public override void Initialize() {
			Bootstrapper.Register(Locator.CurrentMutable, Locator.Current);
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted() {
			if ( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ) {
				desktop.MainWindow = new DashboardWindow {
					DataContext = new DashboardWindowViewModel(Locator.Current.GetService<StateManager>()),
				};
			}
			base.OnFrameworkInitializationCompleted();
		}
	}
}