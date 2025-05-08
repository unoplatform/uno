using System;
using Windows.Media.Core;
using Windows.Media.Playback;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using static Private.Infrastructure.TestServices;
using System.Threading.Tasks;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;
using System.Threading;

#if HAS_UNO
using Uno.Foundation.Extensibility;
using Uno.Media.Playback;
#endif

using _MediaPlayer = Windows.Media.Playback.MediaPlayer; // alias to avoid same name root namespace from ios/macos

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;
#if __WASM__
[Ignore("UNO TODO - This test is failing on WASM [https://github.com/unoplatform/uno/issues/12665]")]
#endif
[TestClass]
[RunsOnUIThread]
public partial class Given_MediaPlayerElement
{
	private static readonly Uri TestVideoUrl = new Uri("https://uno-assets.platform.uno/tests/uno/big_buck_bunny_720p_5mb.mp4");

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/13384")]
	public async Task When_MediaPlayerElement_NotAutoPlay_Source()
	{
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = false,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
			Width = 100,
		};
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut, timeoutMS: 6000);

		// PlaybackState should transition out of Opening state when the video is ready to play.
		await WindowHelper.WaitFor(
			condition: () => sut.MediaPlayer?.PlaybackSession?.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused,
			timeoutMS: 5000,
			message: "Timeout waiting for the media player to enter Paused state."
		);
	}

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/13384")]
	public async Task When_MediaPlayerElement_AutoPlay_Source()
	{
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
		};
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut, timeoutMS: 6000);

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
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement();
		sut.SetMediaPlayer(new Windows.Media.Playback.MediaPlayer());
		Assert.IsNull(sut.Source);
	}

	[TestMethod]
	public async Task When_MediaPlayerElement_Added_In_Opening()
	{
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,

			// Workaround to get the control loaded https://github.com/unoplatform/uno/issues/14735
			AreTransportControlsEnabled = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
		};

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut, timeoutMS: 6000);

		sut.MediaPlayer.Play();

		var mediaTransportControls = sut.TransportControls as MediaTransportControls;
		Assert.IsNotNull(mediaTransportControls);

		var mediaPlayer = sut.MediaPlayer as Windows.Media.Playback.MediaPlayer;
		Assert.IsNotNull(mediaPlayer);
	}

#if __APPLE_UIKIT__ || __ANDROID__ || !HAS_UNO
	// [Ignore("Test ignored on windows. Could not find the element by name. And Not supported under MAC [https://github.com/unoplatform/uno/issues/12663]")]
	// [Ignore("https://github.com/unoplatform/uno/issues/13384")]
	[Ignore("https://github.com/unoplatform/uno/issues/13384")]
#endif
#if __SKIA__
	[Ignore("https://github.com/unoplatform/uno/issues/14735")]
#endif
	[TestMethod]
	public async Task When_MediaPlayerElement_SetIsFullWindow_Check_Fullscreen()
	{
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,

			// Workaround to get the control loaded https://github.com/unoplatform/uno/issues/14735
			AreTransportControlsEnabled = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
		};

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut, timeoutMS: 6000);

		sut.MediaPlayer.Play();

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
#if HAS_UNO
			sut.MediaPlayer.Stop();
#endif
		}
	}

	[TestMethod]
#if __SKIA__
	[Ignore("https://github.com/unoplatform/uno/issues/15471")]
#endif
	public async Task When_MediaPlayerElement_SetSource_Check_Play()
	{
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,

			// Workaround to get the control loaded https://github.com/unoplatform/uno/issues/14735
			AreTransportControlsEnabled = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
		};

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut, timeoutMS: 6000);

		sut.MediaPlayer.Play();
		await WindowHelper.WaitFor(
				condition: () => sut.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing,
				timeoutMS: 3000,
				message: "Timeout waiting for the playback session state changing to Play."
			);
	}

	[TestMethod]
	[Ignore("https://github.com/unoplatform/uno/issues/13384")]
	public async Task When_MediaPlayerElement_SetSource_Check_PlayStop()
	{
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
		};

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut, timeoutMS: 6000);

		// step 1: Test Play
		sut.MediaPlayer.Play();
		await WindowHelper.WaitFor(
					condition: () => sut.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing,
					timeoutMS: 3000,
					message: "Timeout waiting for the playback session state changing to playing on PlayStop."
				);

		// step 2: Test Stop
#if !HAS_UNO
		sut.MediaPlayer.Pause();
		sut.MediaPlayer.Source = null;
#else
		sut.MediaPlayer.Stop();
#endif
		await WindowHelper.WaitFor(
					condition: () => sut.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing,
					timeoutMS: 3000,
					message: "Timeout waiting for the playback session state changing to Stop on PlayStop."
				);
#if !HAS_UNO
		sut.MediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(TestVideoUrl);
#endif

	}

#if __SKIA__
	[Ignore("Test not work properly on CI, the player do not have time to pause, doe the video do not auto play.  [https://github.com/unoplatform/uno/issues/12692]")]
#elif __ANDROID__
	[Ignore("Fails on Android")]
#endif
	[TestMethod]
	public async Task When_MediaPlayerElement_SetSource_Check_PlayPause()
	{
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
		};

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut, timeoutMS: 6000);

		// step 1: Test Play
		sut.MediaPlayer.Play();
		await WindowHelper.WaitFor(
					condition: () => sut.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing,
					timeoutMS: 3000,
					message: "Timeout waiting for the playback session state changing to playing on PlayPause."
				);

		// step 1: Test Pause
		//Needed to GTK change State from Opening to Playing
		await Task.Delay(3000);
		sut.MediaPlayer.Pause();
		await WindowHelper.WaitFor(
			condition: () => sut.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing,
			timeoutMS: 6000,
			message: "Timeout waiting for the playback session state changing to Pause on PlayPause."
		);
	}

#if !HAS_UNO
	[Ignore("Test ignored on windows. Could not find the element by name")]
#endif
	[TestMethod]
	public async Task When_MediaPlayerElement_Check_TransportControlVisibility()
	{
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,

			// Workaround to get the control loaded https://github.com/unoplatform/uno/issues/14735
			AreTransportControlsEnabled = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
		};

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut, timeoutMS: 6000);

		sut.AreTransportControlsEnabled = false;

		var root = (WindowHelper.XamlRoot?.Content as FrameworkElement)!;
		var tcp = (FrameworkElement)root.FindName("TransportControlsPresenter");

		Assert.AreEqual(Visibility.Collapsed, tcp.Visibility);
		sut.AreTransportControlsEnabled = true;
		Assert.AreEqual(Visibility.Visible, tcp.Visibility);
		sut.AreTransportControlsEnabled = false;
		Assert.AreEqual(Visibility.Collapsed, tcp.Visibility);
	}

#if !HAS_UNO
	[Ignore("Test ignored on windows. Could not find the element by name")]
#endif
	[TestMethod]
	public async Task When_MediaPlayerElement_Check_TransportControlButonsVisibility()
	{
		CheckMediaPlayerExtensionAvailability();
		var sut = new MediaPlayerElement()
		{
			AutoPlay = true,
			Source = MediaSource.CreateFromUri(TestVideoUrl),
			AreTransportControlsEnabled = true
		};

		//Load Player
		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut, timeoutMS: 6000);

		// step 1: disalbe ShowAndHideAutomatically
		var root = (WindowHelper.XamlRoot?.Content as FrameworkElement)!;
		sut.TransportControls.ShowAndHideAutomatically = false;

		//// step 2: Make sure all buttons are enabled
		sut.TransportControls.IsFastForwardEnabled = true;
		sut.TransportControls.IsFastRewindEnabled = true;
		sut.TransportControls.IsPlaybackRateEnabled = true;
#if !WINAPPSDK && !__SKIA__ // Full window mode is not supported on Skia yet https://github.com/unoplatform/uno-private/issues/1276
		sut.TransportControls.IsFullWindowEnabled = true;
#endif
		sut.TransportControls.IsRepeatEnabled = true;
		sut.TransportControls.IsSeekEnabled = true;
		sut.TransportControls.IsSkipBackwardEnabled = true;
		sut.TransportControls.IsStopEnabled = true;
		sut.TransportControls.IsVolumeEnabled = true;
		sut.TransportControls.IsZoomEnabled = true;
#if !WINAPPSDK
		sut.TransportControls.IsCompactOverlayEnabled = true;
#endif
		sut.TransportControls.IsSkipForwardEnabled = true;

		//// step 3: Collapsed Visibility from all to make sure that we have space
		sut.TransportControls.IsFastForwardButtonVisible = false;
		sut.TransportControls.IsFastRewindButtonVisible = false;
#if !WINAPPSDK && !__SKIA__ // Full window mode is not supported on Skia yet https://github.com/unoplatform/uno-private/issues/1276
		sut.TransportControls.IsFullWindowButtonVisible = false;
#endif
		sut.TransportControls.IsNextTrackButtonVisible = false;
		sut.TransportControls.IsPlaybackRateButtonVisible = false;
		sut.TransportControls.IsPreviousTrackButtonVisible = false;
		sut.TransportControls.IsRepeatButtonVisible = false;
		sut.TransportControls.IsSeekBarVisible = false;
		sut.TransportControls.IsSkipBackwardButtonVisible = false;
		sut.TransportControls.IsSkipForwardButtonVisible = false;
		sut.TransportControls.IsStopButtonVisible = false;
		sut.TransportControls.IsVolumeButtonVisible = false;
		sut.TransportControls.IsZoomButtonVisible = false;
#if !WINAPPSDK
		sut.TransportControls.IsCompactOverlayButtonVisible = false;
#endif

		// step 4: Start to validate one by one.
		sut.TransportControls.IsFastForwardButtonVisible = true;
		var esut = (FrameworkElement)root.FindName("FastForwardButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls FastForwardButton Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsFastForwardButtonVisible = false;
		esut = (FrameworkElement)root.FindName("FastForwardButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls FastForwardButton Visibility Collapsed when Auto Hide."
				);

		sut.TransportControls.IsFastRewindButtonVisible = true;
		esut = (FrameworkElement)root.FindName("RewindButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls FastRewindButton Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsFastRewindButtonVisible = false;
		esut = (FrameworkElement)root.FindName("RewindButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls FastRewindButton Visibility Collapsed when Auto Hide."
				);

#if !WINAPPSDK && !__SKIA__ // Full window mode is not supported on Skia yet https://github.com/unoplatform/uno-private/issues/1276
		sut.TransportControls.IsFullWindowButtonVisible = true;
		esut = (FrameworkElement)root.FindName("FullWindowButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsFullWindowButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsFullWindowButtonVisible = false;
		esut = (FrameworkElement)root.FindName("FullWindowButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsFullWindowButtonVisible Visibility Collapsed when Auto Hide."
				);
#endif

		sut.TransportControls.IsNextTrackButtonVisible = true;
		esut = (FrameworkElement)root.FindName("NextTrackButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsNextTrackButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsNextTrackButtonVisible = false;
		esut = (FrameworkElement)root.FindName("NextTrackButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsNextTrackButtonVisible Visibility Collapsed when Auto Hide."
				);


#if __SKIA__
		//PlaybackRate is not implemented in Android and IOS
		sut.TransportControls.IsPlaybackRateButtonVisible = true;
		esut = (FrameworkElement)root.FindName("PlaybackRateButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsPlaybackRateButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsPlaybackRateButtonVisible = false;
		esut = (FrameworkElement)root.FindName("PlaybackRateButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsPlaybackRateButtonVisible Visibility Collapsed when Auto Hide."
				);
#endif

		sut.TransportControls.IsPreviousTrackButtonVisible = true;
		esut = (FrameworkElement)root.FindName("PreviousTrackButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsPreviousTrackButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsPreviousTrackButtonVisible = false;
		esut = (FrameworkElement)root.FindName("PreviousTrackButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsPreviousTrackButtonVisible Visibility Collapsed when Auto Hide."
				);

		//Just works on the fluent layout for now. The generic is show
		//Issue: https://github.com/unoplatform/uno/issues/12664
		//sut.TransportControls.IsRepeatButtonVisible = false;
		//esut = (FrameworkElement)root.FindName("RepeatButton");
		//await WindowHelper.WaitFor(
		//			condition: () => esut.Visibility == Visibility.Collapsed,
		//			timeoutMS: 3000,
		//			message: "Timeout waiting for TransportControls IsRepeatButtonVisible Visibility Collapsed when Auto Hide."
		//		);
		//sut.TransportControls.IsRepeatButtonVisible = true;
		//esut = (FrameworkElement)root.FindName("RepeatButton");
		//await WindowHelper.WaitFor(
		//			condition: () => esut.Visibility == Visibility.Visible,
		//			timeoutMS: 3000,
		//			message: "Timeout waiting for TransportControls IsRepeatButtonVisible Visibility Collapsed when Auto Hide."
		//		);

		sut.TransportControls.IsSkipBackwardButtonVisible = true;
		esut = (FrameworkElement)root.FindName("SkipBackwardButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsSkipBackwardButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsSkipBackwardButtonVisible = false;
		esut = (FrameworkElement)root.FindName("SkipBackwardButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsSkipBackwardButtonVisible Visibility Collapsed when Auto Hide."
				);

		sut.TransportControls.IsSkipForwardButtonVisible = true;
		esut = (FrameworkElement)root.FindName("SkipForwardButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsSkipForwardButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsSkipForwardButtonVisible = false;
		esut = (FrameworkElement)root.FindName("SkipForwardButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsSkipForwardButtonVisible Visibility Collapsed when Auto Hide."
				);

		sut.TransportControls.IsStopButtonVisible = true;
		esut = (FrameworkElement)root.FindName("StopButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsStopButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsStopButtonVisible = false;
		esut = (FrameworkElement)root.FindName("StopButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsStopButtonVisible Visibility Collapsed when Auto Hide."
				);

		sut.TransportControls.IsVolumeButtonVisible = true;
		esut = (FrameworkElement)root.FindName("VolumeMuteButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsVolumeButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsVolumeButtonVisible = false;
		esut = (FrameworkElement)root.FindName("VolumeMuteButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsVolumeButtonVisible Visibility Collapsed when Auto Hide."
				);

		sut.TransportControls.IsZoomButtonVisible = true;
		esut = (FrameworkElement)root.FindName("ZoomButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsZoomButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsZoomButtonVisible = false;
		esut = (FrameworkElement)root.FindName("ZoomButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsZoomButtonVisible Visibility Collapsed when Auto Hide."
				);

#if !WINAPPSDK
		sut.TransportControls.IsCompactOverlayButtonVisible = true;
		esut = (FrameworkElement)root.FindName("CompactOverlayButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsCompactOverlayButtonVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsCompactOverlayButtonVisible = false;
		esut = (FrameworkElement)root.FindName("CompactOverlayButton");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsCompactOverlayButtonVisible Visibility Collapsed when Auto Hide."
				);
#endif

		sut.TransportControls.IsSeekBarVisible = true;
		esut = (FrameworkElement)root.FindName("MediaTransportControls_Timeline_Border");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Visible,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsSeekBarVisible Visibility Collapsed when Auto Hide."
				);
		sut.TransportControls.IsSeekBarVisible = false;
		esut = (FrameworkElement)root.FindName("MediaTransportControls_Timeline_Border");
		await WindowHelper.WaitFor(
					condition: () => esut.Visibility == Visibility.Collapsed,
					timeoutMS: 3000,
					message: "Timeout waiting for TransportControls IsSeekBarVisible Visibility Collapsed when Auto Hide."
				);
	}

	private void CheckMediaPlayerExtensionAvailability()
	{
#if HAS_UNO
		if (_MediaPlayer.ImplementedByExtensions && !ApiExtensibility.IsRegistered<IMediaPlayerExtension>())
		{
			Assert.Inconclusive("Platform not supported.");
		}
#endif
	}
}
