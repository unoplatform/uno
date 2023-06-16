using System;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using static Private.Infrastructure.TestServices;
using System.Threading.Tasks;

#if HAS_UNO
using Uno.Foundation.Extensibility;
using Uno.Media.Playback;
#endif

using _MediaPlayer = Windows.Media.Playback.MediaPlayer; // alias to avoid same name root namespace from ios/macos

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public partial class Given_MediaPlayerElement
{
	private static readonly Uri TestVideoUrl = new Uri("https://test-videos.co.uk/vids/bigbuckbunny/mp4/h264/720/Big_Buck_Bunny_720_10s_5MB.mp4");

	[TestMethod]
	public async void When_NotAutoPlay_Source()
	{
		Test_Setup();
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

		WindowHelper.WindowContent = null;
	}

	[TestMethod]
	public async void When_MediaPlayerElement_AutoPlay_Source()
	{
		Test_Setup();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
		};
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);

#if __SKIA__
		// AutoPlay is not working on Skia for now.
		await WindowHelper.WaitFor(
			condition: () => sut.MediaPlayer?.PlaybackSession?.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused,
			timeoutMS: 5000,
			message: "Timeout waiting for the media player to enter Playing state on Auto Play."
		);
#else
		// PlaybackState should transition out of Opening state when the video is ready to play.
		await WindowHelper.WaitFor(
			condition: () => sut.MediaPlayer?.PlaybackSession?.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing,
			timeoutMS: 5000,
			message: "Timeout waiting for the media player to enter Playing state on Auto Play."
		);
#endif
	}

	[TestMethod]
	public void When_MediaPlayerElement_Not_SetSource_Property_Value()
	{
		Test_Setup();
		var sut = new MediaPlayerElement();
		sut.SetMediaPlayer(new Windows.Media.Playback.MediaPlayer());
		Assert.IsNull(sut.Source);
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_Added_In_Opening()
	{
		Test_Setup();
		var sut = new MediaPlayerElement();
		sut.Source = Windows.Media.Core.MediaSource.CreateFromUri(TestVideoUrl);
		sut.AutoPlay = true;

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);

		sut.MediaPlayer.Play();

		var mediaTransportControls = sut.TransportControls as MediaTransportControls;
		Assert.IsNotNull(mediaTransportControls);

		var mediaPlayer = sut.MediaPlayer as Windows.Media.Playback.MediaPlayer;
		Assert.IsNotNull(mediaPlayer);
		WindowHelper.WindowContent = null;
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_SetIsFullWindow_Check_Fullscreen()
	{
		Test_Setup();
		var sut = new MediaPlayerElement();
		sut.Source = Windows.Media.Core.MediaSource.CreateFromUri(TestVideoUrl);
		sut.AutoPlay = true;

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);

		try
		{
			var root = (WindowHelper.XamlRoot?.Content as FrameworkElement)!;
			var mpp = (FrameworkElement)root.FindName("MediaPlayerPresenter");
			var currentWidth = mpp.ActualWidth;

			sut.IsFullWindow = true;
			await Task.Delay(2000);

			if (mpp != null)
			{
				Assert.AreNotEqual(mpp.ActualWidth, currentWidth);
			}
		}
		finally
		{
			sut.IsFullWindow = false;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_SetSource_Check_Play()
	{
		Test_Setup();
		var sut = new MediaPlayerElement();
		sut.Source = Windows.Media.Core.MediaSource.CreateFromUri(TestVideoUrl);
		sut.AutoPlay = true;

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);

		sut.MediaPlayer.Play();
		await WindowHelper.WaitFor(
				condition: () => sut.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing,
				timeoutMS: 3000,
				message: "Timeout waiting for the playback session state changing to Play."
			);
		WindowHelper.WindowContent = null;
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_SetSource_Check_PausePlayStop()
	{
		Test_Setup();
		var sut = new MediaPlayerElement();
		sut.Source = Windows.Media.Core.MediaSource.CreateFromUri(TestVideoUrl);
		sut.AutoPlay = true;

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);

		sut.MediaPlayer.Play();

		await WindowHelper.WaitFor(
					condition: () => sut.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing,
					timeoutMS: 3000,
					message: "Timeout waiting for the playback session state changing to playing."
				);

		// step 2: Test Stop
#if UAP10_0_18362
		sut.MediaPlayer.Pause();
		sut.MediaPlayer.Source = null;
#else
		sut.MediaPlayer.Stop();
#endif
		await WindowHelper.WaitFor(
					condition: () => sut.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing,
					timeoutMS: 3000,
					message: "Timeout waiting for the playback session state changing to Stop."
				);
#if UAP10_0_18362
		sut.MediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(TestVideoUrl);
#endif
		// step 3: Test Pause
		sut.MediaPlayer.Play();
		await Task.Delay(1000);

		sut.MediaPlayer.Pause();
		await Task.Delay(1000);
		await WindowHelper.WaitFor(
			condition: () => sut.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused,
			timeoutMS: 3000,
			message: "Timeout waiting for the playback session state changing to Pause on Check_PausePlayStop."
		);
		WindowHelper.WindowContent = null;
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_Check_TransportControlvisibility()
	{
		Test_Setup();
		var sut = new MediaPlayerElement();
		sut.Source = Windows.Media.Core.MediaSource.CreateFromUri(TestVideoUrl);
		sut.AutoPlay = true;

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);

		var root = (WindowHelper.XamlRoot?.Content as FrameworkElement)!;
		var tcp = (FrameworkElement)root.FindName("TransportControlsPresenter");

		Assert.AreEqual(tcp.Visibility, Visibility.Collapsed);
		sut.AreTransportControlsEnabled = true;
		Assert.AreEqual(tcp.Visibility, Visibility.Visible);
		sut.AreTransportControlsEnabled = false;
		Assert.AreEqual(tcp.Visibility, Visibility.Collapsed);
		WindowHelper.WindowContent = null;
	}

	public void Test_Setup()
	{
#if HAS_UNO
		if (_MediaPlayer.ImplementedByExtensions && !ApiExtensibility.IsRegistered<IMediaPlayerExtension>())
		{
			Assert.Inconclusive("Platform not supported.");
		}
#endif
	}
}
