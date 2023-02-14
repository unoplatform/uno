using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
