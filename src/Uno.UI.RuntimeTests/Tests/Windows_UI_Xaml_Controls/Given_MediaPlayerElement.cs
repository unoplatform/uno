using System;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

#if HAS_UNO
using Uno.Foundation.Extensibility;
using Uno.Media.Playback;
#endif

using _MediaPlayer = Windows.Media.Playback.MediaPlayer; // alias to avoid same name root namespace from ios/macos

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_MediaPlayerElement
	{
		private static readonly Uri TestVideoUrl = new Uri("https://test-videos.co.uk/vids/bigbuckbunny/mp4/h264/720/Big_Buck_Bunny_720_10s_5MB.mp4");

		[TestMethod]
		public async void When_NotAutoPlay_Source()
		{
#if HAS_UNO
			if (_MediaPlayer.ImplementedByExtensions && !ApiExtensibility.IsRegistered<IMediaPlayerExtension>())
			{
				Assert.Inconclusive("Platform not supported.");
			}
#endif

			var sut = new MediaPlayerElement()
			{
				AutoPlay = false,
				Source = MediaSource.CreateFromUri(TestVideoUrl),
			};
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);

			// PlaybackState should transition out of Opening state when the video is ready to play.
			await WindowHelper.WaitFor(
				condition: () => sut.MediaPlayer?.PlaybackSession?.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused,
				timeoutMS: 5000,
				message: "Timeout waiting for the media player to enter Paused state."
			);
		}
	}
}
