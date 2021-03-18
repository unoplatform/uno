#if __ANDROID__ || __IOS__ || __MACOS__
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
			get
			{
				return _position;
			}
			set
			{
				if (value < TimeSpan.Zero)
				{
					_position = TimeSpan.Zero;
				}
				else if (value > NaturalDuration)
				{
					_position = NaturalDuration;
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
				}
				else if (value > NaturalDuration)
				{
					_position = NaturalDuration;
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
			get
			{
				return _naturalDuration;
			}
			internal set
			{
				_naturalDuration = value;
				NaturalDurationChanged?.Invoke(this, _naturalDuration);
			}
		}

		private double _bufferingProgress;
		public double BufferingProgress
		{
			get
			{
				return _bufferingProgress;
			}
			internal set
			{
				_bufferingProgress = value;
				BufferingProgressChanged?.Invoke(this, _bufferingProgress);
			}
		}

		private double _playbackRate;
		public double PlaybackRate
		{
			get
			{
				return _playbackRate;
			}
			set
			{
				_playbackRate = value;
				PlaybackRateChanged?.Invoke(this, _playbackRate);
			}
		}

		private MediaPlaybackState _playbackState;
		public MediaPlaybackState PlaybackState
		{
			get
			{
				return _playbackState;
			}
			set
			{
				_playbackState = value;
				PlaybackStateChanged?.Invoke(this, _playbackState);
			}
		}

		public event TypedEventHandler<MediaPlaybackSession, object> BufferingProgressChanged;

		public event TypedEventHandler<MediaPlaybackSession, object> NaturalDurationChanged;

		public event TypedEventHandler<MediaPlaybackSession, object> PlaybackRateChanged;

		public event TypedEventHandler<MediaPlaybackSession, object> PlaybackStateChanged;

		public event TypedEventHandler<MediaPlaybackSession, object> PositionChanged;

		internal MediaPlaybackSession(Windows.Media.Playback.MediaPlayer mediaPlayer)
		{
			MediaPlayer = mediaPlayer;
		}
	}
}
#endif
