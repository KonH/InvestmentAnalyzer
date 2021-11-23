using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DesktopClient.Views;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using Splat;

namespace InvestmentAnalyzer.DesktopClient {
	public class App : Application {
		public override void Initialize() {
			Bootstrapper.Register(Locator.CurrentMutable, Locator.Current);
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted() {
			if ( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ) {
				desktop.MainWindow = new InitializationWindow {
					ViewModel = new InitializationWindowViewModel(Locator.Current.GetService<StateManager>())
				};
			}
			base.OnFrameworkInitializationCompleted();
		}
	}
}