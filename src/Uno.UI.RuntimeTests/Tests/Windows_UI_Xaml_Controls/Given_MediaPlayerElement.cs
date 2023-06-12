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

	[TestMethod]
	public async void When_MediaPlayerElement_AutoPlay_Source()
	{
#if HAS_UNO
		if (_MediaPlayer.ImplementedByExtensions && !ApiExtensibility.IsRegistered<IMediaPlayerExtension>())
		{
			Assert.Inconclusive("Platform not supported.");
		}
#endif

		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
		};
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);

		// PlaybackState should transition out of Opening state when the video is ready to play.
		await WindowHelper.WaitFor(
			condition: () => sut.MediaPlayer?.PlaybackSession?.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing,
			timeoutMS: 5000,
			message: "Timeout waiting for the media player to enter Paused state."
		);
	}

	[TestMethod]
	public void When_MediaPlayerElement_Not_SetSource_Property_Value()
	{
		var mpe = new MediaPlayerElement();
		mpe.SetMediaPlayer(new Windows.Media.Playback.MediaPlayer());
		Assert.IsNull(mpe.Source);
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_Added_In_Opening()
	{
		MediaPlayerElement mpe = await StartPlayerAsync();

		mpe.MediaPlayer.Play();

		var mediaTransportControls = mpe.TransportControls as MediaTransportControls;
		Assert.IsNotNull(mediaTransportControls);

		var mediaPlayer = mpe.MediaPlayer as Windows.Media.Playback.MediaPlayer;
		Assert.IsNotNull(mediaPlayer);
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_SetIsFullWindow_Check_Fullscreen()
	{

		MediaPlayerElement mpe = await StartPlayerAsync();
		try
		{
			var root = (WindowHelper.XamlRoot?.Content as FrameworkElement)!;
			var mpp = (FrameworkElement)root.FindName("MediaPlayerPresenter");
			var currentWidth = mpp.ActualWidth;

			mpe.IsFullWindow = true;
			await Task.Delay(3000);

			if (mpp != null)
			{
				Assert.AreNotEqual(mpp.ActualWidth, currentWidth);
			}
		}
		finally
		{
			mpe.IsFullWindow = false;
		}
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_SetSource_Check_Play()
	{
		MediaPlayerElement mpe = await StartPlayerAsync();

		mpe.MediaPlayer.Play();
		await WindowHelper.WaitFor(
				condition: () => mpe.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing,
				timeoutMS: 3000,
				message: "Timeout waiting for the playback session state changing to Play."
			);
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_SetSource_Check_PausePlayStop()
	{
		MediaPlayerElement mpe = await StartPlayerAsync();

		mpe.MediaPlayer.Play();

		await WindowHelper.WaitFor(
					condition: () => mpe.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing,
					timeoutMS: 3000,
					message: "Timeout waiting for the playback session state changing to playing."
				);

		// step 2: Test Stop
		mpe.MediaPlayer.Stop();
		//Assert.AreEqual(mpe.MediaPlayer.PlaybackSession.Position, TimeSpan.Zero);
		//await WindowHelper.WaitFor(() => mpe.MediaPlayer.PlaybackSession.Position == TimeSpan.Zero, 3000, "Timeout waiting for the playback session state changing to playing.")
		await WindowHelper.WaitFor(
					condition: () => mpe.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing,
					timeoutMS: 3000,
					message: "Timeout waiting for the playback session state changing to Stop."
				);

		// step 3: Test Pause
		mpe.MediaPlayer.Play();
		await Task.Delay(1000);

		mpe.MediaPlayer.Pause();
		var pausePosition = mpe.MediaPlayer.PlaybackSession.Position;
		await WindowHelper.WaitFor(
			condition: () => mpe.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused,
			timeoutMS: 3000,
			message: "Timeout waiting for the playback session state changing to Pause."
		);
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_Check_TransportControlvisibility()
	{
		MediaPlayerElement mpe = await StartPlayerAsync();
		var root = (WindowHelper.XamlRoot?.Content as FrameworkElement)!;
		var tcp = (FrameworkElement)root.FindName("TransportControlsPresenter");

		Assert.AreEqual(tcp.Visibility, Visibility.Collapsed);
		mpe.AreTransportControlsEnabled = true;
		Assert.AreEqual(tcp.Visibility, Visibility.Visible);
		mpe.AreTransportControlsEnabled = false;
		Assert.AreEqual(tcp.Visibility, Visibility.Collapsed);
	}

	public async Task<MediaPlayerElement> StartPlayerAsync()
	{
		var mpe = new MediaPlayerElement();
		mpe.Source = Windows.Media.Core.MediaSource.CreateFromUri(TestVideoUrl);
		mpe.AutoPlay = true;

		//Load Player
		WindowHelper.WindowContent = mpe;
		await WindowHelper.WaitForLoaded(mpe);
		return mpe;
	}
}
