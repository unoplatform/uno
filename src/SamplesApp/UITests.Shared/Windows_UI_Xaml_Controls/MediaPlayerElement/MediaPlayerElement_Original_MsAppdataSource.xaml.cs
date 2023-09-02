using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Graph;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement
{
	[SampleControlInfo("MediaPlayerElement", "Original using a local ms-appdata source", ignoreInSnapshotTests: true, description: "Video using a local ms-appdata source for video and poster. At the moment media element cannot read content from the in-browser file system, so keep this test aside for WASM.", IsManualTest = true)]
	public sealed partial class MediaPlayerElement_Original_MsAppdataSource : UserControl
	{
		public MediaPlayerElement_Original_MsAppdataSource()
		{
			this.InitializeComponent();
		}

		private async void SelectVideoButton_Click(object sender, RoutedEventArgs e)
		{
			var picker = new FileOpenPicker();
			picker.FileTypeFilter.Add("*");
			StorageFile file = await picker.PickSingleFileAsync();
			var extension = Path.GetExtension(file.Name);
			var fileName = Guid.NewGuid() + extension;
			await file.CopyAsync(ApplicationData.Current.LocalFolder, fileName);
			var uri = new Uri($"ms-appdata:///Local/{fileName}");
			SelectedVideo.Source = MediaSource.CreateFromUri(uri);
			SelectedVideo.Visibility = Visibility.Visible;
		}
	}
}
