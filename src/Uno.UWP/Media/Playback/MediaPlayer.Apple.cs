#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AVFoundation;
using CoreFoundation;
using CoreMedia;
using Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Media.Playback;
using Windows.Media.Core;
using Uno.Helpers;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer : IDisposable
	{
		/// <summary>
		/// The property changed dispatcher for a the iOS MediaPlayer
		/// </summary>
		/// <remarks>
		/// It seems that the AudioToolbox may try to raise some property changed on collected MediaPlayer, which may crash the application.
		/// This object is a lightweight ** leaking ** object which acts as a proxy to receive the callbacks and propagate them
		/// to the target MediaPlayer but only if was not collected yet. This is acceptable only as the MediaPlayer is not a control which
		/// is created frequently and this proxy object is really thin in memory.
		/// 
		/// The stacktrace of the crash report that this object is expected to fix:
		///		Crashed: AVAudioSession Notify Thread
		///		0  AudioToolbox                   0x1a96762c0 AudioSessionPropertyListeners::CallPropertyListenersImp(AudioSessionPropertyListeners const&, unsigned int, unsigned int, void const*) + 456
		///		1  AudioToolbox                   0x1a976c100 AudioSessionPropertyListeners::CallPropertyListeners(unsigned int, unsigned int, void const*) + 220
		///		2  AudioToolbox                   0x1a97aa43c HandleCFPropertyListChange(unsigned int, unsigned int, unsigned long, unsigned char*, unsigned int) + 456
		///		3  AudioToolbox                   0x1a97aea34 HandleAudioSessionCFTypePropertyChangedMessage(unsigned int, unsigned int, void*, unsigned int) + 268
		///		4  AudioToolbox                   0x1a97adcf8 ProcessDeferredMessage(unsigned int, __CFData const*, unsigned int, unsigned int) + 1312
		///		5  AudioToolbox                   0x1a97ad4e4 ASCallbackReceiver_AudioSessionPingMessage + 588
		///		6  AudioToolbox                   0x1a9666ec0 _XAudioSessionPingMessage + 52
		///		7  AudioToolbox                   0x1a99ae1a8 mshMIGPerform + 232
		///		8  CoreFoundation                 0x1a56af690 __CFRUNLOOP_IS_CALLING_OUT_TO_A_SOURCE1_PERFORM_FUNCTION__ + 56
		///		9  CoreFoundation                 0x1a56aeddc __CFRunLoopDoSource1 + 440
		///		10 CoreFoundation                 0x1a56a9c00 __CFRunLoopRun + 2096
		///		11 CoreFoundation                 0x1a56a90b0 CFRunLoopRunSpecific + 436
		///		12 AVFAudio                       0x1ab591334 GenericRunLoopThread::Entry(void*) + 156
		///		13 AVFAudio                       0x1ab5bbc60 CAPThread::Entry(CAPThread*) + 88
		///		14 libsystem_pthread.dylib        0x1a533c2c0 _pthread_body + 128
		///		15 libsystem_pthread.dylib        0x1a533c220 _pthread_start + 44
		///		16 libsystem_pthread.dylib        0x1a533fcdc thread_start + 4
		/// </remarks>
		private class Observer : NSObject
		{
			private readonly WeakReference<MediaPlayer> _target;

			public Observer(MediaPlayer target)
			{
				_target = new WeakReference<MediaPlayer>(target);

				// Here we make sure to create only one instance of each handler delegate
				// and mainly we make sure to hold those instances so they won't be collected.
				OnMediaFailed = OnMediaFailedCore;
				OnMediaStalled = OnMediaStalledCore;
				OnMediaEnded = OnMediaEndedCore;

				// Explicitly leak this object! Cf. Remarks above.
				GCHandle.Alloc(this);
			}

			public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
			{
				if (!_target.TryGetTarget(out var player))
				{
					return;
				}

				switch (keyPath)
				{
					case "status":
						player.OnStatusChanged();
						return;

					case "loadedTimeRanges":
						player.OnBufferingProgressChanged();
						return;

					case "duration":
						player.OnCurrentItemDurationChanged();
						return;

					case "rate":
						player.OnRateChanged();
						return;

					case "videoRect":
						player.OnVideoRectChanged();
						return;
				}
			}

			public EventHandler<AVPlayerItemErrorEventArgs> OnMediaFailed { get; }
			private void OnMediaFailedCore(object? sender, AVPlayerItemErrorEventArgs args)
				=> _target.GetTarget()?.OnMediaFailed(new Exception(args.Error.LocalizedDescription));

			public EventHandler<NSNotificationEventArgs> OnMediaStalled { get; }
			private void OnMediaStalledCore(object? sender, NSNotificationEventArgs args)
				=> _target.GetTarget()?.OnMediaFailed();

			public EventHandler<NSNotificationEventArgs> OnMediaEnded { get; }
			private void OnMediaEndedCore(object? sender, NSNotificationEventArgs args)
				=> _target.GetTarget()?.OnMediaEnded(sender, args);
		}

		private Observer _observer;
		private AVQueuePlayer? _player;
		private AVPlayerLayer _videoLayer;
		private NSObject _periodicTimeObserverObject;
		private NSObject _itemFailedToPlayToEndTimeNotification;
		private NSObject _playbackStalledNotification;
		private NSObject _didPlayToEndTimeNotification;

		private static readonly NSString _rateObservationContext = new NSString("AVCustomEditPlayerViewControllerRateObservationContext");

		const string MsAppXScheme = "ms-appx";

		public IVideoSurface RenderSurface { get; } = new VideoSurface();

		internal uint NaturalVideoHeight { get; private set; }
		internal uint NaturalVideoWidth { get; private set; }

		private void Initialize()
		{
			_observer = new Observer(this);
		}

		// This is necessary for the UIKit-Skia target, specifically for code that
		// compiles against the reference API (which has IsLoopingAllEnabled) such as
		// MediaTransportControls.UpdateRepeatStates().
		[Uno.NotImplemented]
		public bool IsLoopingAllEnabled { get; set; }

		#region Player Initialization

		private void TryDisposePlayer()
		{
			if (_player != null)
			{
				try
				{
					_videoLayer.RemoveObserver(_observer, new NSString("videoRect"), _videoLayer.Handle);
					_videoLayer.RemoveFromSuperLayer();

					_player.CurrentItem?.RemoveObserver(_observer, new NSString("loadedTimeRanges"), _player.Handle);
					_player.CurrentItem?.RemoveObserver(_observer, new NSString("status"), _player.Handle);
					_player.CurrentItem?.RemoveObserver(_observer, new NSString("duration"), _player.Handle);
					_player.RemoveObserver(_observer, new NSString("rate"), _rateObservationContext.Handle);
					_player.RemoveTimeObserver(_periodicTimeObserverObject);
					_player.RemoveAllItems();
				}
				finally
				{
					_itemFailedToPlayToEndTimeNotification?.Dispose();
					_playbackStalledNotification?.Dispose();
					_didPlayToEndTimeNotification?.Dispose();

					_videoLayer?.Dispose();

					_player?.CurrentItem?.Dispose();
					_player?.Dispose();
					_player = null;
				}
			}
		}

		private void InitializePlayer()
		{
			_player = new AVQueuePlayer
			{
				ActionAtItemEnd = AVPlayerActionAtItemEnd.None
			};

			_videoLayer = AVPlayerLayer.FromPlayer(_player);
			_videoLayer.Frame = ((VideoSurface)RenderSurface).Frame;
			_videoLayer.VideoGravity = AVLayerVideoGravity.ResizeAspect;
			((VideoSurface)RenderSurface).Layer.AddSublayer(_videoLayer);
#if __IOS__
			var avSession = AVAudioSession.SharedInstance();
			avSession.SetCategory(AVAudioSessionCategory.Playback);

			avSession.SetActive(true, out var activationError);

			if (activationError is not null && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Could not activate audio session: {activationError.LocalizedDescription}");
			}
#endif
			_videoLayer.AddObserver(_observer, new NSString("videoRect"), NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial, _videoLayer.Handle);
			_player.AddObserver(_observer, new NSString("rate"), NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial, _rateObservationContext.Handle);

			_itemFailedToPlayToEndTimeNotification = AVPlayerItem.Notifications.ObserveItemFailedToPlayToEndTime(_observer.OnMediaFailed);
			_playbackStalledNotification = AVPlayerItem.Notifications.ObservePlaybackStalled(_observer.OnMediaStalled);
			_didPlayToEndTimeNotification = AVPlayerItem.Notifications.ObserveDidPlayToEndTime(_observer.OnMediaEnded);

			_periodicTimeObserverObject = _player.AddPeriodicTimeObserver(new CMTime(1, 4), DispatchQueue.MainQueue, delegate
			{
				if (_player?.CurrentItem == null)
				{
					return;
				}

				if (PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
				{
					PlaybackSession.PositionFromPlayer = Position;
				}
			});
		}

		private void InitializeSource()
		{
			PlaybackSession.NaturalDuration = TimeSpan.Zero;
			PlaybackSession.PositionFromPlayer = TimeSpan.Zero;

			// Reset player
			TryDisposePlayer();

			if (Source == null)
			{
				return;
			}

			try
			{
				InitializePlayer();

				PlaybackSession.PlaybackState = MediaPlaybackState.Opening;

				if (_player?.CurrentItem is { } currentItem)
				{
					currentItem.RemoveObserver(_observer, new NSString("duration"), _player.Handle);
					currentItem.RemoveObserver(_observer, new NSString("status"), _player.Handle);
					currentItem.RemoveObserver(_observer, new NSString("loadedTimeRanges"), _player.Handle);
				}

				switch (Source)
				{
					case MediaPlaybackList playlist:
						Play(playlist);
						break;
					case MediaPlaybackItem item:
						Play(item.Source.Uri);
						break;
					case MediaSource source:
						Play(source.Uri);
						break;
					default:
						throw new InvalidOperationException("Unsupported media source type");
				}

				if (_player?.CurrentItem is { } playerCurrentItem)
				{
					playerCurrentItem.AddObserver(_observer, new NSString("duration"), NSKeyValueObservingOptions.Initial, _player.Handle);
					playerCurrentItem.AddObserver(_observer, new NSString("status"), NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial, _player.Handle);
					playerCurrentItem.AddObserver(_observer, new NSString("loadedTimeRanges"), NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New, _player.Handle);
					playerCurrentItem.SeekingWaitsForVideoCompositionRendering = true;

					// Adapt pitch to prevent "metallic echo" when changing playback rate
					playerCurrentItem.AudioTimePitchAlgorithm = AVAudioTimePitchAlgorithm.TimeDomain;
				}
			}
			catch (Exception ex)
			{
				OnMediaFailed(ex);
			}
		}

		private void Play(MediaPlaybackList playlist)
		{
			if (_player is null)
			{
				return;
			}

			foreach (var item in playlist.Items)
			{
				var asset = AVAsset.FromUrl(DecodeUri(item.Source.Uri));
				_player.InsertItem(new AVPlayerItem(asset), null);
			}
		}

		private void Play(Uri uri)
		{
			if (_player is null)
			{
				return;
			}

			var nsAsset = AVAsset.FromUrl(DecodeUri(uri));
			var streamingItem = AVPlayerItem.FromAsset(nsAsset);

			_player.ReplaceCurrentItemWithPlayerItem(streamingItem);
		}

		private static NSUrl DecodeUri(Uri uri)
		{
			if (!uri.IsAbsoluteUri || uri.Scheme == "")
			{
				uri = new Uri(MsAppXScheme + ":///" + uri.OriginalString.TrimStart('/'));
			}

			if (uri.IsLocalResource())
			{
				var filePath = uri.PathAndQuery.TrimStart('/')
				// UWP supports backward slash in path for directory separators
				.Replace("\\", "/");
				return NSUrl.CreateFileUrl(filePath, relativeToUrl: null);
			}

			if (uri.IsAppData())
			{
				var filePath = AppDataUriEvaluator.ToPath(uri);
				return NSUrl.CreateFileUrl(filePath, relativeToUrl: null);
			}

			if (uri.IsFile)
			{
				return NSUrl.CreateFileUrl(uri.PathAndQuery, relativeToUrl: null);
			}

			return new NSUrl(uri.ToString());
		}

		#endregion

		public void Play()
		{
			if (Source == null || _player == null)
			{
				return;
			}

			try
			{
				if (PlaybackSession.PlaybackState == MediaPlaybackState.None)
				{
					// It's AVPlayer default behavior to clear CurrentItem when no next item exists
					// Solution to this is to reinitialize the source if video was: Ended, Failed or Manually stopped (not paused)
					// This will also reinitialize all videos in case of source list, but only in one of 3 listed scenarios above
					InitializeSource();
				}

				PlaybackSession.PlaybackState = MediaPlaybackState.Buffering;
				_player.Play();
				PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
			}
			catch (Exception ex)
			{
				OnMediaFailed(ex);
			}
		}

		private void OnMediaEnded(object? sender, NSNotificationEventArgs args)
		{
			MediaEnded?.Invoke(this, null);

			if (Source is MediaPlaybackList && args.Notification.Object is AVPlayerItem avPlayerItem && avPlayerItem != _player?.Items.Last())
			{
				return;
			}

			PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		private void OnMediaFailed(Exception? ex = null, string? message = null)
		{
			MediaFailed?.Invoke(
				this,
				new MediaPlayerFailedEventArgs(MediaPlayerError.Unknown, message ?? ex?.Message, ex));

			PlaybackSession.PlaybackState = MediaPlaybackState.None;

			TryDisposePlayer();
		}

		private void OnStatusChanged()
		{
			if (_player?.CurrentItem != null)
			{

				IsVideo = _player.CurrentItem.Tracks.Any(IsVideoTrack);

				if (_player.CurrentItem.Status == AVPlayerItemStatus.Failed || _player.Status == AVPlayerStatus.Failed)
				{
					OnMediaFailed();
					return;
				}

				if (_player.Status == AVPlayerStatus.ReadyToPlay &&
					PlaybackSession.PlaybackState is MediaPlaybackState.Opening or MediaPlaybackState.Buffering)
				{
					if (_player.Rate == 0.0)
					{
						PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
					}
					else
					{
						PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
						_player.Play();
					}
				}

				if (_player.Status == AVPlayerStatus.ReadyToPlay)
				{
					MediaOpened?.Invoke(this, null);
				}
			}

			static bool IsVideoTrack(AVPlayerItemTrack track)
			{
				if (track.AssetTrack is not { } assetTrack)
				{
					return false;
				}

				return assetTrack.FormatDescriptions.Any(x => x.MediaType == CMMediaType.Video);
			}
		}

		private void OnVideoRectChanged()
		{
			if (_videoLayer?.VideoRect is { } videoRect)
			{
				NaturalVideoWidth = (uint)videoRect.Width;
				NaturalVideoHeight = (uint)videoRect.Height;
				NaturalVideoDimensionChanged?.Invoke(this, null);
			}
		}

		private void OnRateChanged()
		{
			if (_player != null)
			{
				var stoppedPlaying = _player.Rate == 0.0;

				if (stoppedPlaying && PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
				{
					//Update the status because the system changed the rate.
					PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
				}
			}
		}

		private void OnBufferingProgressChanged()
		{
			var loadedTimeRanges = _player?.CurrentItem?.LoadedTimeRanges;
			if (loadedTimeRanges == null || loadedTimeRanges.Length == 0 || PlaybackSession.NaturalDuration == TimeSpan.Zero)
			{
				PlaybackSession.BufferingProgress = 0;
			}
			else
			{
				var buffer = loadedTimeRanges.Select(tr => tr.CMTimeRangeValue.Start.Seconds + tr.CMTimeRangeValue.Duration.Seconds).Max() / PlaybackSession.NaturalDuration.TotalSeconds * 100;
				PlaybackSession.BufferingProgress = buffer;
			}
		}

		private void OnCurrentItemDurationChanged()
		{
			var duration = _player?.CurrentItem?.Duration;

			if (duration != CMTime.Indefinite)
			{
				PlaybackSession.NaturalDuration = TimeSpan.FromSeconds(duration?.Seconds ?? 0);
			}
		}

		public void Pause()
		{
			if (PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
			{
				_player?.Pause();
				PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
			}
		}

		public void Stop()
		{
			_player?.Pause();
			_player?.CurrentItem?.Seek(CMTime.Zero);
			PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		private void ToggleMute()
		{
			if (_player != null)
			{
				_player.Muted = IsMuted;
			}
		}

		private void OnVolumeChanged()
		{
			if (_player != null)
			{
				_player.Volume = (float)Volume / 100;
			}
		}

		public TimeSpan Position
		{
			get
			{
				return TimeSpan.FromSeconds(_player?.CurrentItem?.CurrentTime.Seconds ?? 0);
			}
			set
			{
				if (_player?.CurrentItem != null)
				{
					_player.CurrentItem.Seek(CMTime.FromSeconds(value.TotalSeconds, 100), CMTime.Zero, CMTime.Zero, completion: OnSeekCompleted);
				}
			}
		}

		public bool IsVideo { get; set; }

		private void OnSeekCompleted(bool finished)
		{
			if (finished)
			{
				SeekCompleted?.Invoke(this, null);
			}
		}

		internal void UpdateVideoGravity(AVLayerVideoGravity gravity)
		{
			if (_videoLayer != null)
			{
				_videoLayer.VideoGravity = gravity;
			}
		}

		public void Dispose()
		{
			TryDisposePlayer();
		}
	}
}
