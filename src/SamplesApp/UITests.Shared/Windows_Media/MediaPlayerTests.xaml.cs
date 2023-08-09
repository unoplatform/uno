using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Storage;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_Media
{
	[SampleControlInfo("Windows.Media", "MediaPlayer")]
	public sealed partial class MediaPlayerTests : Page
	{
		public MediaPlayerTests()
		{
			this.InitializeComponent();
		}

		public string TestSource => "ms-appx:///Assets/test.mp4";


		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(TestSource));

			var stream = await file.OpenAsync(FileAccessMode.Read);

			mediaPlayer.Source = MediaSource.CreateFromStream(stream, "mp4");
		}
	}
}
