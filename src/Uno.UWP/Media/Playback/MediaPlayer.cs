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
				CurrentStateChanged?.Invoke(this, null);
			}
		}

		public bool AutoPlay { get; set; }

		#endregion

		#region Events

		public event TypedEventHandler<MediaPlayer, object> SourceChanged;

		public event TypedEventHandler<MediaPlayer, object> CurrentStateChanged;

		#endregion
	}
}
#endif
