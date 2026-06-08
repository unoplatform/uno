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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_Media
{
	[Sample("Windows.Media", Name = "MediaPlayer", IgnoreInSnapshotTests = true)]
	public sealed partial class MediaPlayerTests : Page
	{
		public MediaPlayerTests()
		{
			this.InitializeComponent();
		}

		public string SoundToPlay => "ms-appx:///sound.mp3";

		private MediaPlaybackItem ToMediaPlaybackItem(object value)
		{
			if (value is string uriString && Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
			{
				var source = MediaSource.CreateFromUri(uri);
				return new MediaPlaybackItem(source);
			}

			return null;
		}
	}
}
