using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DesktopClient.Views {
	public class StateChooseWindow : Window {
		public StateChooseWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		private void OnOpenClick(object? sender, RoutedEventArgs e) {
			Close(false);
		}

		private void OnCreateClick(object? sender, RoutedEventArgs e) {
			Close(true);
		}
	}
}