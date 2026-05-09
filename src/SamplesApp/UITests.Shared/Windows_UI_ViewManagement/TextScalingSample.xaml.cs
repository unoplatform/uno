using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.ViewManagement;

namespace UITests.Windows_UI_ViewManagement;

[Sample("Windows.UI.ViewManagement", Description = "Demonstrates text scaling with IsTextScaleFactorEnabled")]
public sealed partial class TextScalingSample : Page
{
	private readonly UISettings _uiSettings = new();

	public TextScalingSample()
	{
		this.InitializeComponent();
		UpdateScaleFactor();
	}

	private void OnRefreshClick(object sender, RoutedEventArgs e)
	{
		UpdateScaleFactor();
	}

	private void OnScaleToggled(object sender, RoutedEventArgs e)
	{
		var enabled = ScaleToggle.IsOn;
		ScaledText8.IsTextScaleFactorEnabled = enabled;
		ScaledText10.IsTextScaleFactorEnabled = enabled;
		ScaledText12.IsTextScaleFactorEnabled = enabled;
		ScaledText14.IsTextScaleFactorEnabled = enabled;
		ScaledText16.IsTextScaleFactorEnabled = enabled;
		ScaledText20.IsTextScaleFactorEnabled = enabled;
		ScaledText28.IsTextScaleFactorEnabled = enabled;
		ScaledText36.IsTextScaleFactorEnabled = enabled;
	}

	private void UpdateScaleFactor()
	{
		ScaleFactorText.Text = _uiSettings.TextScaleFactor.ToString("F2");
	}
}
