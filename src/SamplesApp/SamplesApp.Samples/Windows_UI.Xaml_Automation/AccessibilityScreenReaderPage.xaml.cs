using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
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

		// IsOffscreen: Opacity toggling
		private void OnSetOpacityZero(object sender, RoutedEventArgs e)
			=> OpacityTargetButton.Opacity = 0;

		private void OnSetOpacityOne(object sender, RoutedEventArgs e)
			=> OpacityTargetButton.Opacity = 1;

		// IsOffscreen: Ancestor visibility
		private void OnCollapseAncestor(object sender, RoutedEventArgs e)
			=> AncestorPanel.Visibility = Visibility.Collapsed;

		private void OnShowAncestor(object sender, RoutedEventArgs e)
			=> AncestorPanel.Visibility = Visibility.Visible;

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
