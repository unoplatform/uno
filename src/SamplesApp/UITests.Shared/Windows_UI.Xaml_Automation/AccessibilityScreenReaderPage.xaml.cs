using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation
{
	[Sample("Automation", Name = "Accessibility_ScreenReader", Description = "Demonstrates VoiceOver / screen reader accessibility properties: names, headings, landmarks, live regions, and interactive controls")]
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
	}
}
