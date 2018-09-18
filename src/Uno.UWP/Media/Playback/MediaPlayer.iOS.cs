using System;
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
		internal AVPlayer Player { get; private set; }

		private AVPlayerLayer _videoLayer;
		private NSObject _periodicTimeObserverObject;
		private NSObject _itemFailedToPlayToEndTimeNotification;
		private NSObject _didPlayToEndTimeNotification;

		public IVideoSurface RenderSurface { get; } = new VideoSurface();

		#region Player Initialization

		private void TryDisposePlayer()
		{
			if (Player != null)
			{
				try
				{
					_videoLayer.RemoveFromSuperLayer();

					Player.CurrentItem?.RemoveObserver(this, new NSString("loadedTimeRanges"), Player.Handle);
					Player.CurrentItem?.RemoveObserver(this, new NSString("status"), Player.Handle);
					Player.CurrentItem?.RemoveObserver(this, new NSString("duration"), Player.Handle);
					Player.RemoveTimeObserver(_periodicTimeObserverObject);
				}
				finally
				{
					_itemFailedToPlayToEndTimeNotification?.Dispose();
					_didPlayToEndTimeNotification?.Dispose();

					_videoLayer?.Dispose();

					Player?.CurrentItem?.Dispose();
					Player?.Dispose();
					Player = null;
				}
			}
		}

		private void InitializePlayer()
		{
			Player = new AVPlayer();
			_videoLayer = AVPlayerLayer.FromPlayer(Player);
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

			_itemFailedToPlayToEndTimeNotification = AVPlayerItem.Notifications.ObserveItemFailedToPlayToEndTime((sender, args) => OnMediaFailed());
			_didPlayToEndTimeNotification = AVPlayerItem.Notifications.ObserveDidPlayToEndTime((sender, args) => OnMediaEnded());

			_periodicTimeObserverObject = Player.AddPeriodicTimeObserver(new CMTime(1, 4), DispatchQueue.MainQueue, delegate
			{
				if (Player?.CurrentItem == null)
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
			PlaybackSession.Position = TimeSpan.Zero;
			
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

				var nsAsset = AVAsset.FromUrl(new NSUrl(((MediaSource)Source).Uri.ToString()));
				var streamingItem = AVPlayerItem.FromAsset(nsAsset);

				Player.CurrentItem?.RemoveObserver(this, new NSString("duration"), Player.Handle);
				Player.CurrentItem?.RemoveObserver(this, new NSString("status"), Player.Handle);
				Player.CurrentItem?.RemoveObserver(this, new NSString("loadedTimeRanges"), Player.Handle);

				Player.ReplaceCurrentItemWithPlayerItem(streamingItem);

				Player.CurrentItem.AddObserver(this, new NSString("duration"), NSKeyValueObservingOptions.Initial, Player.Handle);
				Player.CurrentItem.AddObserver(this, new NSString("status"), NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial, Player.Handle);
				Player.CurrentItem.AddObserver(this, new NSString("loadedTimeRanges"), NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New, Player.Handle);

				Player.CurrentItem.SeekingWaitsForVideoCompositionRendering = true;

				// Adapt pitch to prevent "metallic echo" when changing playback rate
				Player.CurrentItem.AudioTimePitchAlgorithm = AVAudioTimePitchAlgorithm.TimeDomain;

				// Disable subtitles if any
				var mediaSelectionGroup = Player.CurrentItem.Asset.MediaSelectionGroupForMediaCharacteristic(AVMediaCharacteristic.Legible);
				if (mediaSelectionGroup != null)
				{
					Player.CurrentItem.SelectMediaOption(null, mediaSelectionGroup);
				}
				
				MediaOpened?.Invoke(this, null);
			}
			catch (Exception)
			{
				OnMediaFailed();
				PlaybackSession.PlaybackState = MediaPlaybackState.None;
			}
		}

		#endregion

		public void Play()
		{
			if (Source == null || Player == null)
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
				Player.Play();
				PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
			}
			catch (Exception)
			{
				OnMediaFailed();
				PlaybackSession.PlaybackState = MediaPlaybackState.None;
			}
		}

		private void OnMediaEnded()
		{
			MediaEnded?.Invoke(this, null);
			PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		private void OnMediaFailed()
		{
			MediaFailed?.Invoke(this, new MediaPlayerFailedEventArgs());
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
			}
		}

		private void ObserveStatus()
		{
			if (Player?.CurrentItem != null)
			{
				if (Player.CurrentItem.Status == AVPlayerItemStatus.Failed || Player.Status == AVPlayerStatus.Failed)
				{
					OnMediaFailed();
					return;
				}

				if (Player.Status == AVPlayerStatus.ReadyToPlay && PlaybackSession.PlaybackState == MediaPlaybackState.Buffering)
				{
					//if (Rate == 0.0)
					//{
					//	CurrentState = MediaPlayerStatus.Paused;
					//}
					//else
					//{
					PlaybackSession.PlaybackState = MediaPlaybackState.Playing;
					Player.Play();
					//}
				}
			}
		}

		private void ObserveBufferingProgress()
		{
			var loadedTimeRanges = Player?.CurrentItem?.LoadedTimeRanges;
			if (loadedTimeRanges == null || loadedTimeRanges.Length == 0 || PlaybackSession.NaturalDuration == TimeSpan.Zero)
			{
				PlaybackSession.BufferingProgress = 0;
			}
			else
			{
				PlaybackSession.BufferingProgress = loadedTimeRanges.Select(tr => tr.CMTimeRangeValue.Start.Seconds + tr.CMTimeRangeValue.Duration.Seconds).Max() / PlaybackSession.NaturalDuration.TotalSeconds;
			}
		}

		private void ObserveCurrentItemDuration()
		{
			var duration = Player.CurrentItem.Duration;

			if (duration != CMTime.Indefinite)
			{
				PlaybackSession.NaturalDuration = TimeSpan.FromSeconds(duration.Seconds);
			}
		}

		public void Pause()
		{
			if (PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
			{
				Player?.Pause();
				PlaybackSession.PlaybackState = MediaPlaybackState.Paused;
			}
		}

		public void Stop()
		{
			Player?.Pause();
			Player?.CurrentItem?.Seek(CMTime.Zero);
			PlaybackSession.PlaybackState = MediaPlaybackState.None;
		}

		private void ToggleMute()
		{
			if (Player != null)
			{
				Player.Muted = IsMuted;
			}
		}

		private void OnVolumeChanged()
		{
			if (Player != null)
			{
				Player.Volume = (float)Volume / 100;
			}
		}

		public TimeSpan Position
		{
			get
			{
				return TimeSpan.FromSeconds(Player.CurrentItem.CurrentTime.Seconds);
			}
			set
			{
				if (Player?.CurrentItem != null)
				{
					Player.CurrentItem.Seek(CMTime.FromSeconds(value.TotalSeconds, 100), CMTime.Zero, CMTime.Zero);
				}
			}
		}
	}
}
