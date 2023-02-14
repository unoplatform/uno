using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests
{
	[Sample("AutoSuggestBox")]

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
}
