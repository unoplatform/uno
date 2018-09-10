#if __ANDROID__ || __IOS__

using System;
using Windows.Foundation;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer
	{
		#region Properties

		private IMediaPlaybackSource _source;
		public IMediaPlaybackSource Source
		{
			get
			{
				return _source;
			}
			set
			{
				Pause();

				_source = value;

				if (AutoPlay)
				{
					Play();
				}

				SourceChanged?.Invoke(this, null);
			}
		}

		private MediaPlayerState _currentState = MediaPlayerState.Closed;
		public MediaPlayerState CurrentState
		{
			get
			{
				return _currentState;
			}
			set
			{
				_currentState = value;
				CurrentStateChanged?.Invoke(this, _currentState);
			}
		}

		public bool AutoPlay { get; set; }

		private bool _isMuted = false;
		public bool IsMuted
		{
			get
			{
				return _isMuted;
			}
			set
			{
				_isMuted = value;
				ToggleMute();
				IsMutedChanged?.Invoke(this, _isMuted);
			}
		}

		private double _volume = 1d;
		public double Volume
		{
			get
			{
				return _volume;
			}
			set
			{
				_volume = value;
				OnVolumeChanged();
				VolumeChanged?.Invoke(this, _isMuted);
			}
		}

		#endregion

		#region Events

		public event TypedEventHandler<MediaPlayer, object> SourceChanged;

		public event TypedEventHandler<MediaPlayer, object> CurrentStateChanged;

		public event TypedEventHandler<MediaPlayer, object> IsMutedChanged;

		public event TypedEventHandler<MediaPlayer, object> VolumeChanged;

		#endregion
	}
}
#endif
