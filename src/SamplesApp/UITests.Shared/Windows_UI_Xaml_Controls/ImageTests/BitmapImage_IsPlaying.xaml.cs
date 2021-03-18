using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[Sample("Image", IgnoreInSnapshotTests = true)]
	public sealed partial class BitmapImage_IsPlaying : Page
	{
		public BitmapImage_IsPlaying()
		{
			this.InitializeComponent();
		}

		private void PlayClicked(object sender, RoutedEventArgs e)
		{
			image.Play();
		}

		private void StopClicked(object sender, RoutedEventArgs e)
		{
			image.Stop();
		}
	}
}
