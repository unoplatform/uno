using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.UITests.ImageTests
{
	[Sample("Image")]
	public sealed partial class Image_Invalid : Page
	{
		public Image_Invalid()
		{
			this.InitializeComponent();
		}

		private void HideClick(object sender, RoutedEventArgs e)
		{
			ComparePanel.Visibility = Visibility.Collapsed;
		}
	}
}
