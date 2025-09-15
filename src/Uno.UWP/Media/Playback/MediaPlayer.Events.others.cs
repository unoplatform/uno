#if !(__ANDROID__ || __IOS__ || __TVOS__)
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Uno.Media.Playback;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer
	{
		void RaiseBufferingEnded()
			=> BufferingEnded?.Invoke(this, new object());

		void RaiseBufferingStarted()
			=> BufferingStarted?.Invoke(this, new object());

		void RaiseCurrentStateChanged()
			=> CurrentStateChanged?.Invoke(this, new object());

		void RaiseIsMutedChanged()
			=> IsMutedChanged?.Invoke(this, new object());

		void RaiseMediaEnded()
			=> MediaEnded?.Invoke(this, new object());

		void RaiseMediaFailed(MediaPlayerError error, string? errorMessage, Exception? extendedErrorCode)
			=> MediaFailed?.Invoke(this, new(error, errorMessage, extendedErrorCode));

		void RaiseMediaOpened()
			=> MediaOpened?.Invoke(this, new object());

		void RaisePlaybackMediaMarkerReached(PlaybackMediaMarker playbackMediaMarker)
			=> PlaybackMediaMarkerReached?.Invoke(this, new(playbackMediaMarker));

		void RaiseMediaPlayerRateChanged(double newRate)
			=> MediaPlayerRateChanged?.Invoke(this, new(newRate));

		void RaiseSeekCompleted()
			=> SeekCompleted?.Invoke(this, new object());

		void RaiseSourceChanged()
			=> SourceChanged?.Invoke(this, new object());

		void RaiseSubtitleFrameChanged()
			=> SubtitleFrameChanged?.Invoke(this, new object());

		void RaiseVideoFrameAvailable()
			=> VideoFrameAvailable?.Invoke(this, new object());

		void RaiseNaturalVideoDimensionChanged()
			=> NaturalVideoDimensionChanged?.Invoke(this, null);

		void RaiseVolumeChanged(double? newVolume = null)
		{
			if (newVolume != null)
			{
				_volume = Math.Clamp(newVolume.Value, 0.0, 1.0);
			}
			VolumeChanged?.Invoke(this, null);
		}

		void RaisePositionChanged()
			=> PlaybackSession.Position = _extension?.Position ?? TimeSpan.Zero;

		void NaturalDurationChanged()
			=> PlaybackSession.NaturalDuration = _extension?.NaturalDuration ?? TimeSpan.Zero;

		class MediaPlayerEvents : IMediaPlayerEventsExtension
		{
			private MediaPlayer _owner;

			public MediaPlayerEvents(MediaPlayer owner)
			{
				_owner = owner;
			}

			void IMediaPlayerEventsExtension.RaiseBufferingEnded()
				=> _owner.RaiseBufferingEnded();

			void IMediaPlayerEventsExtension.RaiseBufferingStarted()
				=> _owner.RaiseBufferingStarted();

			void IMediaPlayerEventsExtension.RaiseCurrentStateChanged()
				=> _owner.RaiseCurrentStateChanged();

			void IMediaPlayerEventsExtension.RaiseIsMutedChanged()
				=> _owner.RaiseIsMutedChanged();

			void IMediaPlayerEventsExtension.RaiseMediaEnded()
				=> _owner.RaiseMediaEnded();

			void IMediaPlayerEventsExtension.RaiseMediaFailed(MediaPlayerError error, string? errorMessage, Exception? extendedErrorCode)
				=> _owner.RaiseMediaFailed(error, errorMessage, extendedErrorCode);

			void IMediaPlayerEventsExtension.RaiseMediaOpened()
				=> _owner.RaiseMediaOpened();

			void IMediaPlayerEventsExtension.RaisePlaybackMediaMarkerReached(PlaybackMediaMarker playbackMediaMarker)
				=> _owner.RaisePlaybackMediaMarkerReached(playbackMediaMarker);

			void IMediaPlayerEventsExtension.RaiseMediaPlayerRateChanged(double newRate)
				=> _owner.RaiseMediaPlayerRateChanged(newRate);

			void IMediaPlayerEventsExtension.RaiseSeekCompleted()
				=> _owner.RaiseSeekCompleted();

			void IMediaPlayerEventsExtension.RaiseSourceChanged()
				=> _owner.RaiseSourceChanged();

			void IMediaPlayerEventsExtension.RaiseSubtitleFrameChanged()
				=> _owner.RaiseSubtitleFrameChanged();

			void IMediaPlayerEventsExtension.RaiseVideoFrameAvailable()
				=> _owner.RaiseVideoFrameAvailable();

			void IMediaPlayerEventsExtension.RaiseNaturalVideoDimensionChanged()
				=> _owner.RaiseNaturalVideoDimensionChanged();

			void IMediaPlayerEventsExtension.RaiseVolumeChanged(double? newVolume)
				=> _owner.RaiseVolumeChanged(newVolume);

			public void RaisePositionChanged()
				=> _owner.RaisePositionChanged();

			public void NaturalDurationChanged()
				=> _owner.NaturalDurationChanged();
		}
	}
}
#endif
