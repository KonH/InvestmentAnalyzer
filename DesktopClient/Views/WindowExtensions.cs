using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using InvestmentAnalyzer.State;
using ReactiveUI;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public static class WindowExtensions {
		public static async Task ShowOpenFileDialog(
			this Window window, InteractionContext<OpenFileDialogOptions, string[]> interaction) {
			var input = interaction.Input;
			var dialog = new OpenFileDialog {
				AllowMultiple = input.AllowMultiple
			};
			if ( input.Filter != null ) {
				dialog.Filters.Add(input.Filter);
			}
			var fileNames = await dialog.ShowAsync(window);
			interaction.SetOutput(fileNames ?? Array.Empty<string>());
		}

		public static async Task ShowSaveFileDialog(this Window window, InteractionContext<Unit, string> interaction) {
			var dialog = new SaveFileDialog();
			var fileName = await dialog.ShowAsync(window);
			interaction.SetOutput(fileName ?? string.Empty);
		}

		public static void CloseWindow(this Window window, InteractionContext<Unit, Unit> interaction) {
			window.Close();
		}
	}
}