using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopClient.Views;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using InvestmentAnalyzer.State;
using ReactiveUI;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class MainWindow : ReactiveWindow<MainWindowViewModel> {
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

		async void OnActivated(object? sender, EventArgs e) {
			if ( _isAlreadyActivated ) {
				return;
			}
			_isAlreadyActivated = true;
			if ( DataContext is not MainWindowViewModel viewModel ) {
				return;
			}
			viewModel.ShowOpenFileDialog.RegisterHandler(ShowOpenFileDialog);
			viewModel.ShowSaveFileDialog.RegisterHandler(ShowSaveFileDialog);
			viewModel.ShowChooseStateDialog.RegisterHandler(ShowChooseStateDialog);
			viewModel.CloseWindow.RegisterHandler(CloseWindow);
			viewModel.ShowAddBrokerWindow.RegisterHandler(ShowAddBrokerWindow);
			await viewModel.Initialize();
		}

		async Task ShowOpenFileDialog(InteractionContext<OpenFileDialogOptions, string[]> interaction) {
			var input = interaction.Input;
			var dialog = new OpenFileDialog {
				AllowMultiple = input.AllowMultiple
			};
			if ( input.Filter != null ) {
				dialog.Filters.Add(input.Filter);
			}
			var fileNames = await dialog.ShowAsync(this);
			interaction.SetOutput(fileNames ?? Array.Empty<string>());
		}

		async Task ShowSaveFileDialog(InteractionContext<Unit, string> interaction) {
			var dialog = new SaveFileDialog();
			var fileName = await dialog.ShowAsync(this);
			interaction.SetOutput(fileName ?? string.Empty);
		}

		async Task ShowChooseStateDialog(InteractionContext<Unit, bool> interaction) {
			var chooseDialog = new StateChooseWindow();
			var shouldCreate = await chooseDialog.ShowDialog<bool>(this);
			interaction.SetOutput(shouldCreate);
		}

		void CloseWindow(InteractionContext<Unit, Unit> interaction) {
			Close();
		}

		async Task ShowAddBrokerWindow(InteractionContext<Unit, BrokerState?> interaction) {
			var addBrokerDialog = new AddBrokerWindow {
				ViewModel = new AddBrokerWindowViewModel()
			};
			var result = await addBrokerDialog.ShowDialog<BrokerState?>(this);
			interaction.SetOutput(result);
		}
	}
}