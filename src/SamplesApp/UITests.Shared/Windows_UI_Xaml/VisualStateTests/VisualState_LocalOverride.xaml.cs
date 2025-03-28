using Microsoft.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Windows_UI_Xaml.VisualStateTests;

[Sample("Visual states", Description = "Background should start off as red, but turn blue after the button is clicked.", IsManualTest = true)]
public sealed partial class VisualState_LocalOverride : Page
{
	public VisualState_LocalOverride()
	{
		this.InitializeComponent();
	}

	private void SetColorButton_Click(object sender, RoutedEventArgs e)
	{
		RootGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
	}
}
