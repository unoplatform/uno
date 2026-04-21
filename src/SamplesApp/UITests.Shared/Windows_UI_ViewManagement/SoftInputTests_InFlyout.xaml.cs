using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.ViewManagement;

namespace UITests.Windows_UI_ViewManagement;

[Sample("Windows.UI.ViewManagement", Name = "SoftInput_InFlyout", Description = "TextBox inside a Flyout. Flyout should NOT dismiss when keyboard appears.")]
public sealed partial class SoftInputTests_InFlyout : Page
{
	public SoftInputTests_InFlyout()
	{
		this.InitializeComponent();
		this.Loaded += (_, _) =>
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing += (s, e) => UpdateStatus(s.OccludedRect, "Showing");
			inputPane.Hiding += (s, e) => UpdateStatus(s.OccludedRect, "Hiding");
		};
	}

	private void UpdateStatus(Windows.Foundation.Rect rect, string evt) =>
		StatusText.Text = $"InputPane: {evt} | OccludedRect: {rect}";
}
