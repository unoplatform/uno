using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.ViewManagement;

namespace UITests.Windows_UI_ViewManagement;

[Sample("Windows.UI.ViewManagement", Name = "SoftInput_InScrollViewer", Description = "TextBox inside ScrollViewer with tall content. Should scroll into view when keyboard appears.")]
public sealed partial class SoftInputTests_InScrollViewer : Page
{
	public SoftInputTests_InScrollViewer()
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
