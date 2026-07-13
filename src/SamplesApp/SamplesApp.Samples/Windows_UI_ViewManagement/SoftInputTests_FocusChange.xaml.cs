using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.ViewManagement;

namespace UITests.Windows_UI_ViewManagement;

[Sample("Windows.UI.ViewManagement", Name = "SoftInput_FocusChange", Description = "Multiple TextBoxes - focus change while keyboard is showing should re-scroll.")]
public sealed partial class SoftInputTests_FocusChange : Page
{
	public SoftInputTests_FocusChange()
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
