#if __ANDROID__ || __IOS__

using System;
using System.Timers;
using Uno.Extensions;
using Uno.Logging;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls : Control
	{
		private Windows.Media.Playback.MediaPlayer _mediaPlayer;

		internal void SetMediaPlayer(Windows.Media.Playback.MediaPlayer mediaPlayer)
		{
			UnbindMediaPlayer();

			_mediaPlayer = mediaPlayer;

			if (_playPauseButton != null)
			{
				BindMediaPlayer();
			}
		}

		private void BindMediaPlayer()
		{
			_mediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
			_mediaPlayer.PlaybackSession.BufferingProgressChanged += OnBufferingProgressChanged;
			_mediaPlayer.PlaybackSession.NaturalDurationChanged += OnNaturalDurationChanged;
			_mediaPlayer.PlaybackSession.PositionChanged += OnPositionChanged;

			_playPauseButton.Click += PlayPause;
			_playPauseButtonOnLeft.Click += PlayPause;
			_audioMuteButton.Click += ToggleMute;
			_volumeSlider.ValueChanged += OnVolumeChanged;
			_stopButton.Click += Stop;
		}

		private void UnbindMediaPlayer()
		{
			try
			{
				_mediaPlayer.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
				_mediaPlayer.PlaybackSession.BufferingProgressChanged -= OnBufferingProgressChanged;
				_mediaPlayer.PlaybackSession.NaturalDurationChanged -= OnNaturalDurationChanged;
				_mediaPlayer.PlaybackSession.PositionChanged -= OnPositionChanged;

				_playPauseButton.Click -= PlayPause;
				_playPauseButtonOnLeft.Click -= PlayPause;
				_audioMuteButton.Click -= ToggleMute;
				_volumeSlider.ValueChanged -= OnVolumeChanged;
				_stopButton.Click -= Stop;
			}
			catch (Exception ex)
			{
				this.Log().ErrorIfEnabled(() => $"Unable to unbing MediaTransportControls properly: {ex.Message}", ex);
			}
		}

		private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
		{
			var state = (MediaPlaybackState)args;
			
			switch (state)
			{
				case MediaPlaybackState.Paused:
				case MediaPlaybackState.None:
					CancelControlsVisibilityTimer();
					VisualStateManager.GoToState(this, "PlayState", false);
					break;
				case MediaPlaybackState.Playing:
				case MediaPlaybackState.Buffering:
				case MediaPlaybackState.Opening:
					ResetControlsVisibilityTimer();
					VisualStateManager.GoToState(this, "PauseState", false);
					break;
			}
		}

		private void OnBufferingProgressChanged(MediaPlaybackSession sender, object args)
		{
		}

		private void OnNaturalDurationChanged(MediaPlaybackSession sender, object args)
		{
			Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var duration = args as TimeSpan?;
				if (duration.HasValue)
				{
					_timeRemainingElement.Text = $"{duration.Value.TotalHours:0}:{duration.Value.Minutes:00}:{duration.Value.Seconds:00}";
					_progressSlider.Minimum = 0;
					_progressSlider.Maximum = duration.Value.TotalSeconds;
				}
				else
				{
					_timeRemainingElement.Text = "0:00:00";
					_progressSlider.Minimum = 0;
					_progressSlider.Maximum = 0;
				}
			});
		}

		private void OnPositionChanged(MediaPlaybackSession sender, object args)
		{
			Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var elapsed = args as TimeSpan?;
				if (elapsed.HasValue)
				{
					_timeElapsedElement.Text = $"{elapsed.Value.TotalHours:0}:{elapsed.Value.Minutes:00}:{elapsed.Value.Seconds:00}";
					_progressSlider.Value = elapsed.Value.TotalSeconds;
				}
				else
				{
					_timeElapsedElement.Text = "0:00:00";
					_progressSlider.Value = 0;
				}
			});
		}

		private void PlayPause(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Buffering || _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
			{
				_mediaPlayer.Pause();
			}
			else
			{
				_mediaPlayer.Play();
			}
		}

		private void Stop(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.Stop();
		}

		private void OnVolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			_mediaPlayer.Volume = e.NewValue;
			VisualStateManager.GoToState(this, _mediaPlayer.Volume == 0 ? "MuteState" : "VolumeState", false);
			ResetControlsVisibilityTimer();
		}

		private void ToggleMute(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.IsMuted = !_mediaPlayer.IsMuted;
			VisualStateManager.GoToState(this, _mediaPlayer.IsMuted ? "MuteState" : "VolumeState", false);
			ResetControlsVisibilityTimer();
		}
	}
}
#endif
