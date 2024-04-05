using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[Sample("Image", IgnoreInSnapshotTests = true)]
	public sealed partial class BitmapImage_IsPlaying : Page, INotifyPropertyChanged
	{
		public BitmapImage_IsPlaying()
		{
			this.InitializeComponent();
			this.Loaded += BitmapImage_IsPlaying_Loaded;
			image.ImageOpened += (s, e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAnimatedBitmap)));
		}

		private void BitmapImage_IsPlaying_Loaded(object sender, RoutedEventArgs e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAnimatedBitmap)));

		public bool IsPlaying => image.IsPlaying;

		public bool IsAnimatedBitmap => image.IsAnimatedBitmap;

		public event PropertyChangedEventHandler PropertyChanged;

		private void PlayClicked(object sender, RoutedEventArgs e)
		{
			image.Play();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
		}

		private void StopClicked(object sender, RoutedEventArgs e)
		{
			image.Stop();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
		}
	}
}
