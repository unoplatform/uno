using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Security.AccessControl;
using Windows.Foundation;

namespace Windows.Media.Playback
{
	public sealed partial class MediaPlayer
	{
		internal const bool ImplementedByExtensions =
#if __ANDROID__ || __IOS__ || __TVOS__
			false;
#else
			true;
#endif

		#region Properties

		private IMediaPlaybackSource _source;
		public IMediaPlaybackSource Source
		{
			get => _source;
			set
			{
				Stop();

				_source = value;

				InitializeSource();
				SourceChanged?.Invoke(this, null);

				if (_source != null && AutoPlay)
				{
					Play();
				}
			}
		}

		public bool AutoPlay { get; set; }

		private bool _isMuted;
		public bool IsMuted
		{
			get => _isMuted;
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
			get => _volume;
			set
			{
				_volume = Math.Clamp(value, 0.0, 1.0);
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

		public event TypedEventHandler<MediaPlayer, object> MediaOpened;

		public event TypedEventHandler<MediaPlayer, object> SeekCompleted;

		public event TypedEventHandler<MediaPlayer, object> NaturalVideoDimensionChanged;

		#endregion

		public MediaPlayer()
		{
			PlaybackSession = new MediaPlaybackSession(this);
			Initialize();
		}

		internal void SetOption(string name, object value)
		{
			OnOptionChanged(name, value);
		}

		partial void OnOptionChanged(string name, object value);

		internal void SetTransportControlBounds(Rect bounds)
		{
			OnTransportControlBoundsChanged(bounds);
		}

		partial void OnTransportControlBoundsChanged(Rect bounds);
	}
}
