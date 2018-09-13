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
				VolumeChanged?.Invoke(this, _volume);
			}
		}

		public MediaPlaybackSession PlaybackSession { get; }

		#endregion

		#region Events

		public event TypedEventHandler<MediaPlayer, object> SourceChanged;
		
		public event TypedEventHandler<MediaPlayer, object> IsMutedChanged;

		public event TypedEventHandler<MediaPlayer, object> VolumeChanged;

		public event TypedEventHandler<MediaPlayer, object> MediaEnded;

		public event TypedEventHandler<MediaPlayer, MediaPlayerFailedEventArgs> MediaFailed;

		#endregion

		public MediaPlayer()
		{
			PlaybackSession = new MediaPlaybackSession(this);
		}
	}
}
#endif
