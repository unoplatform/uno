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
		private bool _isScrubbing = false;

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
			_skipForwardButton.Click += SkipForward;
			_skipBackwardButton.Click += SkipBackward;

			AttachThumbEventHandlers(_progressSlider);
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
				_skipForwardButton.Click -= SkipForward;
				_skipBackwardButton.Click -= SkipBackward;

				DetachThumbEventHandlers(_progressSlider);
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
				case MediaPlaybackState.Opening:
				case MediaPlaybackState.Paused:
				case MediaPlaybackState.None:
					CancelControlsVisibilityTimer();
					VisualStateManager.GoToState(this, "PlayState", false);
					VisualStateManager.GoToState(this, "Normal", false);
					break;
				case MediaPlaybackState.Playing:
					ResetControlsVisibilityTimer();
					VisualStateManager.GoToState(this, "PauseState", false);
					VisualStateManager.GoToState(this, "Normal", false);
					break;
				case MediaPlaybackState.Buffering:
					VisualStateManager.GoToState(this, "Buffering", false);
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
				var duration = args as TimeSpan? ?? TimeSpan.Zero;
				_progressSlider.Minimum = 0;
				_progressSlider.Maximum = duration.TotalSeconds;

				if (_mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing && _mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Paused)
				{
					_timeRemainingElement.Text = $"{duration.TotalHours:0}:{duration.Minutes:00}:{duration.Seconds:00}";
				}
			});
		}

		private void OnPositionChanged(MediaPlaybackSession sender, object args)
		{
			Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var elapsed = args as TimeSpan? ?? TimeSpan.Zero;
				_timeElapsedElement.Text = $"{elapsed.TotalHours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
				_progressSlider.Value = elapsed.TotalSeconds;

				var remaining = _mediaPlayer.PlaybackSession.NaturalDuration - elapsed;
				_timeRemainingElement.Text = $"{remaining.TotalHours:0}:{remaining.Minutes:00}:{remaining.Seconds:00}";
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

		private void SkipBackward(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.Position - TimeSpan.FromSeconds(10);
		}

		private void SkipForward(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.Position + TimeSpan.FromSeconds(30);
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

		private void AttachThumbEventHandlers(Slider slider)
		{
			var thumb = slider.GetTemplateChild(HorizontalThumbName) as Thumb;

			if (thumb != null)
			{
				thumb.DragStarted -= ThumbOnDragStarted;
				thumb.DragStarted += ThumbOnDragStarted;

				thumb.DragCompleted -= ThumbOnDragCompleted;
				thumb.DragCompleted += ThumbOnDragCompleted;
			}
		}

		private void DetachThumbEventHandlers(Slider slider)
		{
			var thumb = slider.GetTemplateChild(HorizontalThumbName) as Thumb;

			if (thumb != null)
			{
				thumb.DragStarted -= ThumbOnDragStarted;
				thumb.DragStarted += ThumbOnDragStarted;

				thumb.DragCompleted -= ThumbOnDragCompleted;
				thumb.DragCompleted += ThumbOnDragCompleted;
			}
		}

		private void ThumbOnDragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
		{
			if (double.IsNaN(_progressSlider.Value))
			{
				return;
			}

			if (_mediaPlayer != null)
			{
				_mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(_progressSlider.Value);

				if (_wasPlaying)
				{
					_mediaPlayer.Play();
				}
			}

			_isScrubbing = false;
		}

		private void ThumbOnDragStarted(object sender, DragStartedEventArgs dragStartedEventArgs)
		{
			if (_mediaPlayer != null && !_isScrubbing)
			{
				_wasPlaying = _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Buffering || _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
				_mediaPlayer.Pause();

				_isScrubbing = true;
			}
		}
	}
}
#endif
