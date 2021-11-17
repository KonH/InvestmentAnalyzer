using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DesktopClient.ViewModels;

namespace DesktopClient.Views {
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		private async void OnInitialized(object? sender, EventArgs e) {
			var dialog = new OpenFileDialog {
				AllowMultiple = false
			};
			dialog.Filters.Add(new FileDialogFilter { Extensions = new List<string> { "zip" } });
			var result = await dialog.ShowAsync(this);
			var path = result?.First() ?? string.Empty;
			var viewModel = (MainWindowViewModel)DataContext!;
			viewModel.SelectedPath = path;
			await viewModel.Initialize();
		}
	}
}