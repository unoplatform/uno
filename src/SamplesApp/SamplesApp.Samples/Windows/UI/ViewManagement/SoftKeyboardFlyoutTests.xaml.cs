using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_ViewManagement
{
	// Manual, on-device sample for flyouts opened near the bottom of the window while the on-screen keyboard is up:
	// the flyout must be placed above the keyboard, and shrunk to the area above it when it is too tall.
	[Sample("Windows.UI.ViewManagement", Description = "On-device checks for flyouts placed above the on-screen keyboard.", IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class SoftKeyboardFlyoutTests : Page
	{
		private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
		private FrameworkElement _openFlyoutContent;

		public SoftKeyboardFlyoutTests()
		{
			this.InitializeComponent();

			SearchList.ItemsSource = Enumerable.Range(0, 30).Select(i => $"Item {i}").ToList();
			TallList.ItemsSource = Enumerable.Range(0, 30).Select(i => $"Item {i}").ToList();

			_timer.Tick += (_, _) => UpdatePresenterState();
			this.Loaded += OnLoaded;
			this.Unloaded += OnUnloaded;
		}

		private void OnFlyoutOpened(object sender, object e)
		{
			_openFlyoutContent = ReferenceEquals(sender, TallFlyout) ? TallFlyoutContent : SearchFlyoutContent;
			_timer.Start();
			UpdatePresenterState();
		}

		private void OnFlyoutClosed(object sender, object e)
		{
			_openFlyoutContent = null;
			_timer.Stop();
			UpdatePresenterState();
		}

		// The presenter is the flyout content's FlyoutPresenter ancestor; its bounds are what the keyboard can hide.
		private void UpdatePresenterState()
		{
			var occludedRect = InputPane.GetForCurrentView().OccludedRect;
			OccludedRectTextBlock.Text = $"OccludedRect: {occludedRect}";

			if (_openFlyoutContent is null || FindPresenter(_openFlyoutContent) is not { } presenter)
			{
				PresenterTextBlock.Text = "Presenter: (no flyout open)";
				return;
			}

			var bounds = presenter.TransformToVisual(null).TransformBounds(new Rect(0, 0, presenter.ActualWidth, presenter.ActualHeight));
			var aboveKeyboard = occludedRect.Height <= 0 || bounds.Bottom <= occludedRect.Y + 0.5;
			PresenterTextBlock.Text =
				$"Presenter: top {bounds.Top:0}, bottom {bounds.Bottom:0}, height {bounds.Height:0}   Above keyboard: {(aboveKeyboard ? "yes" : "NO")}";
		}

		private static FlyoutPresenter FindPresenter(DependencyObject element)
		{
			for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
			{
				if (current is FlyoutPresenter presenter)
				{
					return presenter;
				}
			}

			return null;
		}

		private void OnInputPaneChanged(InputPane sender, InputPaneVisibilityEventArgs args) => UpdatePresenterState();

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing += OnInputPaneChanged;
			inputPane.Hiding += OnInputPaneChanged;
			UpdatePresenterState();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing -= OnInputPaneChanged;
			inputPane.Hiding -= OnInputPaneChanged;
			_timer.Stop();
		}
	}
}
