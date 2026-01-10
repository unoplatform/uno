using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement
{
	[Sample("MediaPlayerElement", "Playlist", ignoreInSnapshotTests: true, description: "Test the playlist and the Repeat")]
	public sealed partial class MediaPlayerElement_Playlist : UserControl
	{
		public MediaPlayerElement_Playlist()
		{
			this.InitializeComponent();
			InitializePlaybackList();
		}

		private void InitializePlaybackList()
		{
			var mediaPlaybackList = new MediaPlaybackList();
			mediaPlaybackList.Items.Add(new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri("https://uno-assets.platform.uno/tests/videos/Mobile_Development_in_VS_Code_with_Uno_Platform_orDotNetMAUI.mp4"))));
			mediaPlaybackList.Items.Add(new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri("https://uno-assets.platform.uno/tests/audio/Getting_Started_with_Uno_Platform_and_Visual_Studio_Code.mp3"))));
			mediaPlaybackList.Items.Add(new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri("https://uno-assets.platform.uno/tests/videos/Getting_Started_with_Uno_Platform_and_Visual_Studio_Code.mp4"))));
			if (mpe.MediaPlayer == null)
			{
				mpe.SetMediaPlayer(new Windows.Media.Playback.MediaPlayer());
			}
			mpe.MediaPlayer.Source = mediaPlaybackList;
		}
	}
}
