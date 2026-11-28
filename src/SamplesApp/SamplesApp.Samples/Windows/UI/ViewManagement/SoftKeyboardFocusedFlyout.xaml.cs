using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;
using Windows.UI.ViewManagement;

namespace UITests.Windows_UI_ViewManagement
{
	// Manual, on-device sample: a flyout that focuses a TextBox when it opens must stay open while the on-screen
	// keyboard comes up, on the page and inside a ContentDialog.
	[Sample("Windows.UI.ViewManagement", Description = "On-device check that a flyout focusing its TextBox on open stays open when the on-screen keyboard shows.", IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class SoftKeyboardFocusedFlyout : Page
	{
		private Window _window;

		public SoftKeyboardFocusedFlyout()
		{
			this.InitializeComponent();

			PageList.ItemsSource = CreateItems();

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private static string[] CreateItems() => Enumerable.Range(0, 20).Select(i => $"Item {i}").ToArray();

		private void OnFlyoutOpened(object sender, object e)
		{
			Log($"{FlyoutName(sender)} Opened");
			if ((sender as Flyout)?.Content is Panel panel && panel.Children.OfType<TextBox>().FirstOrDefault() is { } searchBox)
			{
				searchBox.Focus(FocusState.Programmatic);
			}
		}

		private void OnFlyoutClosed(object sender, object e) => Log($"{FlyoutName(sender)} Closed");

		private string FlyoutName(object flyout) => ReferenceEquals(flyout, PageFlyout) ? "Page flyout" : "Dialog flyout";

		private async void OnOpenDialog(object sender, RoutedEventArgs e)
		{
			var flyout = new Flyout { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft };
			var panel = new StackPanel { Width = 280, Spacing = 8 };
			panel.Children.Add(new TextBox { PlaceholderText = "Search..." });
			panel.Children.Add(new ListView { MaxHeight = 240, ItemsSource = CreateItems() });
			flyout.Content = panel;
			flyout.Opened += OnFlyoutOpened;
			flyout.Closed += OnFlyoutClosed;

			var dialog = new ContentDialog
			{
				XamlRoot = XamlRoot,
				Title = "Dialog",
				Content = new DropDownButton { Content = "Pick (in dialog)", Flyout = flyout },
				CloseButtonText = "Close",
			};

			await dialog.ShowAsync();
		}

		private void OnClearLog(object sender, RoutedEventArgs e) => LogTextBlock.Text = "";

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			XamlRoot.Changed += OnXamlRootChanged;
#if HAS_UNO
			// HostWindow is Uno-only; on WinUI the activation is not logged.
			_window = XamlRoot.HostWindow;
#endif
			if (_window is not null)
			{
				_window.Activated += OnWindowActivated;
			}

			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing += OnInputPaneChanged;
			inputPane.Hiding += OnInputPaneChanged;
			Log($"Loaded, XamlRoot size {XamlRoot.Size}");
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			if (XamlRoot is { } xamlRoot)
			{
				xamlRoot.Changed -= OnXamlRootChanged;
			}

			if (_window is not null)
			{
				_window.Activated -= OnWindowActivated;
				_window = null;
			}

			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing -= OnInputPaneChanged;
			inputPane.Hiding -= OnInputPaneChanged;
		}

		private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => Log($"XamlRoot.Changed, size {sender.Size}");

		private void OnWindowActivated(object sender, WindowActivatedEventArgs args) => Log($"Window.Activated {args.WindowActivationState}");

		private void OnInputPaneChanged(InputPane sender, InputPaneVisibilityEventArgs args) => Log($"InputPane occluded {sender.OccludedRect}");

		private void Log(string message)
		{
			var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
			Console.WriteLine("[SoftKeyboardFocusedFlyout] " + line);
			LogTextBlock.Text = line + "\n" + LogTextBlock.Text;
		}
	}
}
