using Microsoft.UI.Xaml;
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
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		var inputPane = InputPane.GetForCurrentView();
		inputPane.Showing += OnInputPaneShowing;
		inputPane.Hiding += OnInputPaneHiding;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		var inputPane = InputPane.GetForCurrentView();
		inputPane.Showing -= OnInputPaneShowing;
		inputPane.Hiding -= OnInputPaneHiding;
	}

	private void OnInputPaneShowing(InputPane sender, InputPaneVisibilityEventArgs args) =>
		UpdateStatus(sender.OccludedRect, "Showing");

	private void OnInputPaneHiding(InputPane sender, InputPaneVisibilityEventArgs args) =>
		UpdateStatus(sender.OccludedRect, "Hiding");

	private void UpdateStatus(Windows.Foundation.Rect rect, string evt) =>
		StatusText.Text = $"InputPane: {evt} | OccludedRect: {rect}";
}
