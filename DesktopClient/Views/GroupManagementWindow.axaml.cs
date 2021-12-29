using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvestmentAnalyzer.DesktopClient.Services;
using InvestmentAnalyzer.DesktopClient.ViewModels;
using ReactiveUI;
using Splat;

namespace InvestmentAnalyzer.DesktopClient.Views {
	public partial class GroupManagementWindow : ReactiveWindow<GroupManagementWindowViewModel> {
		public GroupManagementWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
			this.WhenActivated(_ => {
					ViewModel?.ShowNewGroupDialog.RegisterHandler(ShowNewGroupDialog);
					ViewModel?.ShowNewEntryDialog.RegisterHandler(ShowNewEntryDialog);
				}
			);
		}

		void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		async Task ShowNewGroupDialog(InteractionContext<Unit, string?> interaction) {
			var dialog = new AddGroupWindow {
				ViewModel = new()
			};
			var result = await dialog.ShowDialog<string>(this);
			interaction.SetOutput(result);
		}

		async Task ShowNewEntryDialog(InteractionContext<Unit, string?> interaction) {
			var dialog = new AddGroupEntryWindow {
				ViewModel = new(Locator.Current.GetService<StateManager>())
			};
			var result = await dialog.ShowDialog<string>(this);
			interaction.SetOutput(result);
		}
	}
}