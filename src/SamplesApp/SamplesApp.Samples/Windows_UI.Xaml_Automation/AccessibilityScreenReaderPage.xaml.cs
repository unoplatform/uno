using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation
{
	[Sample("Automation", Name = "Accessibility_ScreenReader", Description = "Demonstrates accessibility properties: names, headings, landmarks, live regions, IsOffscreen, IsEnabled, IsKeyboardFocusable")]
	public sealed partial class AccessibilityScreenReaderPage : UserControl
	{
		private int _statusCounter;

		public AccessibilityScreenReaderPage()
		{
			this.InitializeComponent();
			Loaded += OnPageLoaded;
		}

		private void OnPageLoaded(object sender, RoutedEventArgs e)
		{
			// Visibility and ancestor-visibility changes only settle on a later layout pass, so
			// reading IsOffscreen() synchronously in the click handler would observe stale bounds.
			// Refreshing on every layout pass keeps every readout correct after layout has run.
			// Opacity and scrolling are render-only (no layout), so those are refreshed from their
			// own handlers (opacity) and the ScrollViewer's ViewChanged (scrolling).
			// Remove-then-add keeps a single subscription if the control is reloaded.
			LayoutUpdated -= OnLayoutUpdated;
			LayoutUpdated += OnLayoutUpdated;
			RefreshAllOffscreenReadouts();
		}

		private void OnLayoutUpdated(object sender, object e)
			=> RefreshAllOffscreenReadouts();

		private void RefreshAllOffscreenReadouts()
		{
			UpdateOffscreenReadout(VisibilityTargetButton, VisibilityOffscreenResult);
			UpdateOffscreenReadout(OpacityTargetButton, OpacityOffscreenResult);
			UpdateOffscreenReadout(AncestorChildButton, AncestorOffscreenResult);
			UpdateOffscreenReadout(ClippedTargetButton, ClippedOffscreenResult);
		}

		// Reads IsOffscreen straight from an element's automation peer so each IsOffscreen
		// scenario can be validated in-app, without inspect.exe or Accessibility Insights.
		private static void UpdateOffscreenReadout(UIElement target, TextBlock result)
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(target);
			var text = $"IsOffscreen = {peer?.IsOffscreen() ?? true}";

			// Only assign when changed: setting Text invalidates layout, which would re-enter
			// OnLayoutUpdated — the guard lets it settle after one pass instead of looping.
			if (result.Text != text)
			{
				result.Text = text;
			}
		}

		private void OnUpdateLiveRegion(object sender, RoutedEventArgs e)
		{
			_statusCounter++;
			LiveRegionText.Text = $"Status: Updated ({_statusCounter})";
		}

		// IsOffscreen: Visibility toggling
		private void OnCollapseTarget(object sender, RoutedEventArgs e)
			=> VisibilityTargetButton.Visibility = Visibility.Collapsed;

		private void OnShowTarget(object sender, RoutedEventArgs e)
			=> VisibilityTargetButton.Visibility = Visibility.Visible;

		// IsOffscreen: Opacity toggling (render-only — no layout pass, so refresh directly)
		private void OnSetOpacityZero(object sender, RoutedEventArgs e)
		{
			OpacityTargetButton.Opacity = 0;
			UpdateOffscreenReadout(OpacityTargetButton, OpacityOffscreenResult);
		}

		private void OnSetOpacityOne(object sender, RoutedEventArgs e)
		{
			OpacityTargetButton.Opacity = 1;
			UpdateOffscreenReadout(OpacityTargetButton, OpacityOffscreenResult);
		}

		// IsOffscreen: Ancestor visibility (readout refreshed by OnLayoutUpdated after layout)
		private void OnCollapseAncestor(object sender, RoutedEventArgs e)
			=> AncestorPanel.Visibility = Visibility.Collapsed;

		private void OnShowAncestor(object sender, RoutedEventArgs e)
			=> AncestorPanel.Visibility = Visibility.Visible;

		// IsOffscreen: Clipping (scrolled out of a ScrollViewer's viewport)
		private void OnScrollTargetOut(object sender, RoutedEventArgs e)
			=> ClippingScrollViewer.ChangeView(null, 500, null, disableAnimation: true);

		private void OnScrollTargetIntoView(object sender, RoutedEventArgs e)
			=> ClippingScrollViewer.ChangeView(null, 0, null, disableAnimation: true);

		private void OnClippingViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
			=> UpdateOffscreenReadout(ClippedTargetButton, ClippedOffscreenResult);

		private void OnCheckClippedOffscreen(object sender, RoutedEventArgs e)
			=> UpdateOffscreenReadout(ClippedTargetButton, ClippedOffscreenResult);

		// IsEnabled / IsKeyboardFocusable toggling
		private void OnDisableTarget(object sender, RoutedEventArgs e)
			=> EnabledTargetButton.IsEnabled = false;

		private void OnEnableTarget(object sender, RoutedEventArgs e)
			=> EnabledTargetButton.IsEnabled = true;

		// AllowFocusWhenDisabled
		private void OnDisableFocusWhenDisabled(object sender, RoutedEventArgs e)
			=> FocusWhenDisabledButton.IsEnabled = false;

		private void OnEnableFocusWhenDisabled(object sender, RoutedEventArgs e)
			=> FocusWhenDisabledButton.IsEnabled = true;

		// Combined state scenarios
		private void OnDisableCombined(object sender, RoutedEventArgs e)
			=> CombinedTextBox.IsEnabled = false;

		private void OnEnableCombined(object sender, RoutedEventArgs e)
			=> CombinedTextBox.IsEnabled = true;

		private void OnCollapseCombined(object sender, RoutedEventArgs e)
			=> CombinedTextBox.Visibility = Visibility.Collapsed;

		private void OnShowCombined(object sender, RoutedEventArgs e)
			=> CombinedTextBox.Visibility = Visibility.Visible;
	}
}
