using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[Sample("Image")]
	public sealed partial class Image_Transforms : Page
	{
		public Image_Transforms()
		{
			this.InitializeComponent();
		}

		private void Turn(object sender, RoutedEventArgs e) => rotate.Angle += 45d;

		private void Turn2(object sender, RoutedEventArgs e) => rotate2.Angle += 45d;

		private void Turn3(object sender, RoutedEventArgs e) => rotate3.Angle += 45d;
	}
}
