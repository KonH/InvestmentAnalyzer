using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopClient.Views;
using InvestmentAnalyzer.DesktopClient.ViewModels;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class MainWindow : ReactiveWindow<MainWindow> {
		bool _isAlreadyActivated = false;

		public MainWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		async Task RunInitializationFlow() {
			if ( _isAlreadyActivated ) {
				return;
			}
			_isAlreadyActivated = true;
			if ( DataContext is not MainWindowViewModel viewModel ) {
				return;
			}
			await viewModel.LoadStartup();
			var isInitializedWithDefaults = await viewModel.TryInitialize();
			if ( isInitializedWithDefaults ) {
				return;
			}
			var chooseDialog = new StateChooseWindow();
			var shouldCreate = await chooseDialog.ShowDialog<bool>(this);
			if ( shouldCreate ) {
				var saveFileDialog = new SaveFileDialog {
					DefaultExtension = "zip"
				};
				var result = await saveFileDialog.ShowAsync(this);
				var path = result ?? string.Empty;
				var isInitialized = await viewModel.InitializeWithPath(path, true);
				if ( isInitialized ) {
					return;
				}
			} else {
				var openFileDialog = new OpenFileDialog {
					AllowMultiple = false
				};
				openFileDialog.Filters.Add(new FileDialogFilter { Extensions = new List<string> { "zip" } });
				var result = await openFileDialog.ShowAsync(this);
				var path = result?.First() ?? string.Empty;
				var isInitialized = await viewModel.InitializeWithPath(path, false);
				if ( isInitialized ) {
					return;
				}
			}
			Close();
		}

		async void OnActivated(object? sender, EventArgs e) {
			await RunInitializationFlow();
		}
	}
}