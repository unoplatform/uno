using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.ViewManagement;

namespace UITests.Windows_UI_ViewManagement;

[Sample("Windows.UI.ViewManagement", Name = "SoftInput_NoScrollViewer", Description = "TextBox at bottom of page with no ScrollViewer. RootScrollViewer should scroll into view.")]
public sealed partial class SoftInputTests_NoScrollViewer : Page
{
	public SoftInputTests_NoScrollViewer()
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
