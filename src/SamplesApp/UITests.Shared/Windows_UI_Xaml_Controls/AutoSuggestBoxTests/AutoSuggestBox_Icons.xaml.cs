using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests;

[Sample("AutoSuggestBox", IsManualTest = true,
	Description =
		"""
		- All inputs except for first one should have an icon on the right.
		- Clicking the button should change the color of the last icon once.
		"""
	)]

public sealed partial class AutoSuggestBox_Icons : Page
{
	public AutoSuggestBox_Icons()
	{
		this.InitializeComponent();
	}

	public void SwitchIconClick(object sender, RoutedEventArgs e)
	{
		var bitmapIcon = new BitmapIcon() { UriSource = new System.Uri("ms-appx:///Assets/RedSquare.png"), ShowAsMonochrome = false };
		BitmapIconBox.QueryIcon = bitmapIcon;
	}
}
