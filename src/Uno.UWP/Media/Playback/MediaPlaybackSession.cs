using System;
using Windows.Foundation;

namespace Windows.Media.Playback
{
	public partial class MediaPlaybackSession
	{
		public Windows.Media.Playback.MediaPlayer MediaPlayer { get; }

		private TimeSpan _position;
		public TimeSpan Position
		{
			get => _position;
			set
			{
				if (value < TimeSpan.Zero)
				{
					_position = TimeSpan.Zero;
					UpdateTimePositionRate = 1;
				}
				else if (value > NaturalDuration)
				{
					_position = NaturalDuration;
					UpdateTimePositionRate = 1;
				}
				else
				{
					_position = value;
				}

				MediaPlayer.Position = _position;
				PositionChanged?.Invoke(this, _position);
			}
		}

		internal TimeSpan PositionFromPlayer
		{
			set
			{
				if (value < TimeSpan.Zero)
				{
					_position = TimeSpan.Zero;
					UpdateTimePositionRate = 1;
				}
				else if (value > NaturalDuration)
				{
					_position = NaturalDuration;
					UpdateTimePositionRate = 1;
				}
				else
				{
					_position = value;
				}

				PositionChanged?.Invoke(this, _position);
			}
		}

		private TimeSpan _naturalDuration;
		public TimeSpan NaturalDuration
		{
			get => _naturalDuration;
			internal set
			{
				_naturalDuration = value;
				NaturalDurationChanged?.Invoke(this, _naturalDuration);
			}
		}

		public bool IsUpdateTimePosition { get; set; }

		public double UpdateTimePositionRate { get; set; }

		private double _bufferingProgress;
		public double BufferingProgress
		{
			get => _bufferingProgress;
			internal set
			{
				_bufferingProgress = value;
				BufferingProgressChanged?.Invoke(this, _bufferingProgress);
			}
		}

		private double _playbackRate;
		public double PlaybackRate
		{
			get => _playbackRate;
			set
			{
				_playbackRate = value;
				PlaybackRateChanged?.Invoke(this, _playbackRate);
			}
		}

		private MediaPlaybackState _playbackState;
		public MediaPlaybackState PlaybackState
		{
			get => _playbackState;
			set
			{
				_playbackState = value;
				PlaybackStateChanged?.Invoke(this, _playbackState);
			}
		}

		internal bool IsPlaying => PlaybackState
			is MediaPlaybackState.Playing
			or MediaPlaybackState.Buffering;

		public event TypedEventHandler<MediaPlaybackSession, object>? BufferingProgressChanged;

		public event TypedEventHandler<MediaPlaybackSession, object>? NaturalDurationChanged;

		public event TypedEventHandler<MediaPlaybackSession, object>? PlaybackRateChanged;

		public event TypedEventHandler<MediaPlaybackSession, object>? PlaybackStateChanged;

		public event TypedEventHandler<MediaPlaybackSession, object>? PositionChanged;

		internal MediaPlaybackSession(Windows.Media.Playback.MediaPlayer mediaPlayer)
		{
			MediaPlayer = mediaPlayer;
		}
	}
}
