using System;
using System.IO;
using System.Linq;
using AVFoundation;
using CoreFoundation;
using CoreMedia;
using Foundation;
using Uno.Extensions;
using Uno.Logging;
using Uno.Media.Playback;
using Windows.Media.Core;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer : NSObject
	{
		private AVPlayer _player;
		private AVPlayerLayer _videoLayer;
		private NSObject _periodicTimeObserverObject;
		private NSObject _itemFailedToPlayToEndTimeNotification;
		private NSObject _playbackStalledNotification;
		private NSObject _didPlayToEndTimeNotification;

		public static NSString RateObservationContext = new NSString("AVCustomEditPlayerViewControllerRateObservationContext");

		const string MsAppXScheme = "ms-appx";
		const string MsAppDataScheme = "ms-appdata";

		public IVideoSurface RenderSurface { get; } = new VideoSurface();

		private void Initialize()
		{
		}

		#region Player Initialization

		private void TryDisposePlayer()
		{
			if (_player != null)
			{
				try
				{
					_videoLayer.RemoveObserver(this, new NSString("videoRect"), _videoLayer.Handle);
					_videoLayer.RemoveFromSuperLayer();

					_player.CurrentItem?.RemoveObserver(this, new NSString("loadedTimeRanges"), _player.Handle);
					_player.CurrentItem?.RemoveObserver(this, new NSString("status"), _player.Handle);
					_player.CurrentItem?.RemoveObserver(this, new NSString("duration"), _player.Handle);
					_player.RemoveObserver(this, new NSString("rate"), RateObservationContext.Handle);
					_player.RemoveTimeObserver(_periodicTimeObserverObject);
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
			_player = new AVPlayer();
			_videoLayer = AVPlayerLayer.FromPlayer(_player);
			_videoLayer.Frame = ((VideoSurface)RenderSurface).Frame;
			_videoLayer.VideoGravity = AVLayerVideoGravity.ResizeAspect;
			((VideoSurface)RenderSurface).Layer.AddSublayer(_videoLayer);

			var avSession = AVAudioSession.SharedInstance();
			avSession.SetCategory(AVAudioSessionCategory.Playback);

			NSError activationError = null;
			avSession.SetActive(true, out activationError);
			if (activationError != null)
			{
				this.Log().WarnIfEnabled(() => $"Could not activate audio session: {activationError.LocalizedDescription}");
			}

			_videoLayer.AddObserver(this, new NSString("videoRect"), NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial, _videoLayer.Handle);
			_player.AddObserver(this, new NSString("rate"), NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial, RateObservationContext.Handle);

			_itemFailedToPlayToEndTimeNotification = AVPlayerItem.Notifications.ObserveItemFailedToPlayToEndTime((sender, args) => OnMediaFailed(new Exception(args.Error.LocalizedDescription)));
			_playbackStalledNotification = AVPlayerItem.Notifications.ObservePlaybackStalled((sender, args) => OnMediaFailed());
			_didPlayToEndTimeNotification = AVPlayerItem.Notifications.ObserveDidPlayToEndTime((sender, args) => OnMediaEnded());

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

		protected virtual void InitializeSource()
		{
			PlaybackSession.NaturalDuration = TimeSpan.Zero;
			PlaybackSession.PositionFromPlayer = TimeSpan.Zero;
			
			if (Source == null)
			{
				return;
			}

			try
			{
				// Reset player
				TryDisposePlayer();
				InitializePlayer();

				PlaybackSession.PlaybackState = MediaPlaybackState.Opening;

				var nsAsset = AVAsset.FromUrl(DecodeUri(((MediaSource)Source).Uri));
				var streamingItem = AVPlayerItem.FromAsset(nsAsset);

				_player.CurrentItem?.RemoveObserver(this, new NSString("duration"), _player.Handle);
				_player.CurrentItem?.RemoveObserver(this, new NSString("status"), _player.Handle);
				_player.CurrentItem?.RemoveObserver(this, new NSString("loadedTimeRanges"), _player.Handle);

				_player.ReplaceCurrentItemWithPlayerItem(streamingItem);

				_player.CurrentItem.AddObserver(this, new NSString("duration"), NSKeyValueObservingOptions.Initial, _player.Handle);
				_player.CurrentItem.AddObserver(this, new NSString("status"), NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial, _player.Handle);
				_player.CurrentItem.AddObserver(this, new NSString("loadedTimeRanges"), NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New, _player.Handle);

				_player.CurrentItem.SeekingWaitsForVideoCompositionRendering = true;

				// Adapt pitch to prevent "metallic echo" when changing playback rate
				_player.CurrentItem.AudioTimePitchAlgorithm = AVAudioTimePitchAlgorithm.TimeDomain;
				
				MediaOpened?.Invoke(this, null);

			}
			catch (Exception ex)
			{
				OnMediaFailed(ex);
			}
		}

		private static NSUrl DecodeUri(Uri uri)
		{
			if (!uri.IsAbsoluteUri || uri.Scheme == "")
			{
				uri = new Uri(MsAppXScheme + ":///" + uri.OriginalString.TrimStart(new char[] { '/' }));
			}

			var isResource = uri.Scheme.Equals(MsAppXScheme, StringComparison.OrdinalIgnoreCase)
							|| uri.Scheme.Equals(MsAppDataScheme, StringComparison.OrdinalIgnoreCase);

			if (isResource)
			{
				var file = uri.PathAndQuery.TrimStart(new[] { '/' });
				var fileName = Path.GetFileNameWithoutExtension(file);
				var fileExtension = Path.GetExtension(file)?.Replace(".", "");
				return NSBundle.MainBundle.GetUrlForResource(fileName, fileExtension);
			}

			if (uri.IsFile)
			{
				return new NSUrl(uri.PathAndQuery);
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
				// If we reached the end of media, we need to reset position to 0
				if (PlaybackSession.PlaybackState == MediaPlaybackState.None)
				{
					PlaybackSession.Position = TimeSpan.Zero;
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

		private void OnMediaEnded()
		{
			MediaEnded?.Invoke(this, null);
			PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		private void OnMediaFailed(Exception ex = null, string message = null)
		{
			MediaFailed?.Invoke(this, new MediaPlayerFailedEventArgs()
			{
				Error = MediaPlayerError.Unknown,
				ExtendedErrorCode = ex,
				ErrorMessage = message ?? ex?.Message
			});

			PlaybackSession.PlaybackState = MediaPlaybackState.None;

			TryDisposePlayer();
		}

		public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
		{
			switch (keyPath)
			{
				case "status":
					ObserveStatus();
					return;

				case "loadedTimeRanges":
					ObserveBufferingProgress();
					return;

				case "duration":
					ObserveCurrentItemDuration();
					return;

				case "rate":
					ObserveRate();
					return;

				case "videoRect":
					ObserveVideoRect();
					return;
			}
		}

		private void ObserveStatus()
		{
			if (_player?.CurrentItem != null)
			{
				if (_player.CurrentItem.Status == AVPlayerItemStatus.Failed || _player.Status == AVPlayerStatus.Failed)
				{
					OnMediaFailed();
					return;
				}

				if (_player.Status == AVPlayerStatus.ReadyToPlay && PlaybackSession.PlaybackState == MediaPlaybackState.Buffering)
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
			}
		}

		private void ObserveVideoRect()
		{
			if (_videoLayer?.VideoRect != null)
			{
				VideoRatioChanged?.Invoke(this, _videoLayer.VideoRect.Width / Math.Max(_videoLayer.VideoRect.Height, 1));
			}
		}

		private void ObserveRate()
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

		private void ObserveBufferingProgress()
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

		private void ObserveCurrentItemDuration()
		{
			var duration = _player.CurrentItem.Duration;

			if (duration != CMTime.Indefinite)
			{
				PlaybackSession.NaturalDuration = TimeSpan.FromSeconds(duration.Seconds);
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
				return TimeSpan.FromSeconds(_player.CurrentItem.CurrentTime.Seconds);
			}
			set
			{
				if (_player?.CurrentItem != null)
				{
					_player.CurrentItem.Seek(CMTime.FromSeconds(value.TotalSeconds, 100), CMTime.Zero, CMTime.Zero, completion: OnSeekCompleted);
				}
			}
		}

		private void OnSeekCompleted(bool finished)
		{
			if (finished)
			{
				SeekCompleted?.Invoke(this, null);
			}
		}

		internal void UpdateVideoGravity(AVLayerVideoGravity gravity)
		{
			_videoLayer.VideoGravity = gravity;
		}

		protected override void Dispose(bool disposing)
		{
			TryDisposePlayer();
		}
	}
}
