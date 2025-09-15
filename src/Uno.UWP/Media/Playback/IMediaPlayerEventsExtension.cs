#nullable enable

using System;
using Windows.Media.Playback;

namespace Uno.Media.Playback
{
	/// <summary>
	/// Extension interface for <see cref="MediaPlayer"/> events
	/// </summary>
	public interface IMediaPlayerEventsExtension
	{
		/// <summary>
		/// Raises the <see cref="MediaPlayer.SourceChanged"/> event
		/// </summary>
		void RaiseSourceChanged();

		/// <summary>
		/// Raises the <see cref="MediaPlayer.IsMutedChanged"/> event
		/// </summary>
		void RaiseIsMutedChanged();

		/// <summary>
		///	Raises the <see cref="MediaPlayer.VolumeChanged"/> event
		///	Optionally, updates the <see cref="MediaPlayer.Volume"/> property
		/// </summary>
		/// <param name="newVolume">The new volume, in case an external volume changed was detected.</param>
		void RaiseVolumeChanged(double? newVolume = null);

		/// <summary>
		///	Raises the <see cref="MediaPlayer.MediaEnded"/> event
		/// </summary>
		void RaiseMediaEnded();

		/// <summary>
		/// Raises the <see cref="MediaPlayer.MediaFailed"/> event
		/// </summary>
		void RaiseMediaFailed(MediaPlayerError error, string? ErrorMessage, Exception? ExtendedErrorCode);

		/// <summary>
		/// Raises the <see cref="MediaPlayer.MediaOpened"/> event
		/// </summary>
		void RaiseMediaOpened();

		/// <summary>
		/// Raises the <see cref="MediaPlayer.SeekCompleted"/> event
		/// </summary>
		void RaiseSeekCompleted();

		/// <summary>
		/// Raises the <see cref="MediaPlayer.NaturalVideoDimensionChanged"/> event
		/// </summary>
		void RaiseNaturalVideoDimensionChanged();

		/// <summary>
		/// Raises the <see cref="MediaPlayer.BufferingEnded"/> event
		/// </summary>
		void RaiseBufferingEnded();

		/// <summary>
		/// Raises the <see cref="MediaPlayer.BufferingStarted"/> event
		/// </summary>
		void RaiseBufferingStarted();

		/// <summary>
		/// Raises the <see cref="MediaPlayer.CurrentStateChanged"/> event
		/// </summary>
		void RaiseCurrentStateChanged();

		/// <summary>
		/// Raises the <see cref="MediaPlayer.MediaPlayerRateChanged"/> event
		/// </summary>
		void RaiseMediaPlayerRateChanged(double newRate);

		/// <summary>
		/// Raises the <see cref="MediaPlayer.PlaybackMediaMarkerReached"/> event
		/// </summary>
		void RaisePlaybackMediaMarkerReached(PlaybackMediaMarker playbackMediaMarker);

		/// <summary>
		/// Raise the <see cref="MediaPlayer.VideoFrameAvailable"/> event
		/// </summary>
		void RaiseVideoFrameAvailable();

		/// <summary>
		/// Raises the <see cref="MediaPlayer.SubtitleFrameChanged"/> event
		/// </summary>
		void RaiseSubtitleFrameChanged();

		void RaisePositionChanged();

		void NaturalDurationChanged();
	}
}
