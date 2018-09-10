using System;
using AVFoundation;
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

		public IVideoSurface RenderSurface { get; } = new VideoSurface();

		#region Player Initialization

		private void TryDisposePlayer()
		{
			if (_player != null)
			{
				try
				{
					_videoLayer.RemoveFromSuperLayer();
				}
				finally
				{
					_videoLayer?.Dispose();
					_player?.CurrentItem?.Dispose();
					_player?.Dispose();
					_player = null;
				}
			}
		}
		private void InitializePlayer()
		{
			TryDisposePlayer();

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
		}

		#endregion

		public void Play()
		{
			if (Source == null)
			{
				return;
			}

			if (_player != null && CurrentState == MediaPlayerState.Paused)
			{
				//We are simply paused so just start again
				CurrentState = MediaPlayerState.Playing;
				_player.Play();
				return;
			}

			try
			{
				InitializePlayer();
				
				CurrentState = MediaPlayerState.Buffering;

				var nsAsset = AVAsset.FromUrl(new NSUrl(((MediaSource)Source).Uri.ToString()));
				var streamingItem = AVPlayerItem.FromAsset(nsAsset);

				_player.CurrentItem?.RemoveObserver(this, new NSString("status"), _player.Handle);

				_player.ReplaceCurrentItemWithPlayerItem(streamingItem);

				_player.CurrentItem.AddObserver(this, new NSString("status"), NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial, _player.Handle);

				_player.CurrentItem.SeekingWaitsForVideoCompositionRendering = true;

				// Adapt pitch to prevent "metallic echo" when changing playback rate
				_player.CurrentItem.AudioTimePitchAlgorithm = AVAudioTimePitchAlgorithm.TimeDomain;

				// Disable subtitles if any
				var mediaSelectionGroup = _player.CurrentItem.Asset.MediaSelectionGroupForMediaCharacteristic(AVMediaCharacteristic.Legible);
				if (mediaSelectionGroup != null)
				{
					_player.CurrentItem.SelectMediaOption(null, mediaSelectionGroup);
				}
				
				_player.Play();
			}
			catch (Exception)
			{
				//OnMediaFailed(new MediaFailedEventArgs(ex.Message, ex));
				CurrentState = MediaPlayerState.Stopped;
			}
		}

		public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
		{
			switch (keyPath)
			{
				case "status":
					ObserveStatus();
					return;
			}
		}

		private void ObserveStatus()
		{
			if (_player?.CurrentItem != null)
			{
				if (_player.CurrentItem.Status == AVPlayerItemStatus.Failed)
				{
					//OnMediaFileFailed();
					return;
				}

				if ((_player.Status == AVPlayerStatus.ReadyToPlay) && (CurrentState == MediaPlayerState.Buffering))
				{
					//if (Rate == 0.0)
					//{
					//	CurrentState = MediaPlayerStatus.Paused;
					//}
					//else
					//{
					CurrentState = MediaPlayerState.Playing;
					_player.Play();
					//}
				}
				else if (_player.Status == AVPlayerStatus.Failed)
				{
					//OnMediaFailed();
					CurrentState = MediaPlayerState.Closed;
				}
			}
		}

		public void Pause()
		{
			if (CurrentState == MediaPlayerState.Playing)
			{
				_player.Pause();
				CurrentState = MediaPlayerState.Paused;
			}
		}

		private void ToggleMute()
		{
			_player.Muted = IsMuted;
		}

		private void OnVolumeChanged()
		{
			_player.Volume = (float)Volume / 100;
		}
	}
}
