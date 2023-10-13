#nullable enable

using System;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Media.Playback;

namespace Uno.Media.Playback
{
	/// <summary>
	/// Extension definition for the <see cref="MediaPlayer"/> class
	/// </summary>
	public interface IMediaPlayerExtension : IDisposable
	{
		/// <summary>
		/// Provides access to the ability to raise MediaPlayer events
		/// </summary>
		IMediaPlayerEventsExtension? Events { get; set; }

		/// <summary>
		/// Gets or sets the playback rate
		/// </summary>
		double PlaybackRate { get; set; }

		/// <summary>
		/// Gets or sets the looping mode
		/// </summary>
		bool IsLoopingEnabled { get; set; }

		/// <summary>
		/// Gets or sets the looping all mode
		/// </summary>
		bool IsLoopingAllEnabled { get; set; }

		/// <summary>
		/// Gets the current player state
		/// </summary>
		MediaPlayerState CurrentState { get; }

		/// <summary>
		/// Gets the natural duration of the current media
		/// </summary>
		TimeSpan NaturalDuration { get; }

		/// <summary>
		/// Gets the protected state of the current media
		/// </summary>
		bool IsProtected { get; }

		/// <summary>
		/// Gets the buffering progress state of the current media
		/// </summary>
		double BufferingProgress { get; }

		/// <summary>
		/// Determines if the current media can be paused
		/// </summary>
		bool CanPause { get; }

		/// <summary>
		/// Determines if the current media can be seeked
		/// </summary>
		bool CanSeek { get; }

		/// <summary>
		/// Gets the audio device type
		/// </summary>
		MediaPlayerAudioDeviceType AudioDeviceType { get; set; }

		/// <summary>
		/// Gets the audio category
		/// </summary>
		MediaPlayerAudioCategory AudioCategory { get; set; }

		/// <summary>
		///	Gets position offset
		/// </summary>
		TimeSpan TimelineControllerPositionOffset { get; set; }

		/// <summary>
		/// Determines if the current media is playing in real time
		/// </summary>
		bool RealTimePlayback { get; set; }

		/// <summary>
		/// Gets or sets the audio balance
		/// </summary>
		double AudioBalance { get; set; }

		/// <summary>
		/// Gets or sets the current position in the media
		/// </summary>
		TimeSpan Position { get; set; }

		/// <summary>
		/// Determines if the current media content is video
		/// </summary>
		bool? IsVideo { get; }

		/// <summary>
		/// Sets the transport controls bounds so that video can be displayed around controls
		/// </summary>
		void SetTransportControlsBounds(Rect bounds);

		/// <summary>
		/// Sets the source from a Uri
		/// </summary>
		void SetUriSource(Uri value);

		/// <summary>
		/// Sets the source from a storage file
		/// </summary>
		void SetFileSource(IStorageFile file);

		/// <summary>
		/// Sets the source from a stream
		/// </summary>
		void SetStreamSource(IRandomAccessStream stream);

		/// <summary>
		/// Sets the source from a media source
		/// </summary>
		void SetMediaSource(IMediaSource source);

		/// <summary>
		/// Steps the media forward one frame
		/// </summary>
		void StepForwardOneFrame();

		/// <summary>
		/// Steps the media backward one frame
		/// </summary>
		void StepBackwardOneFrame();

		/// <summary>
		/// Sets the surface size
		/// </summary>
		void SetSurfaceSize(Size size);

		/// <summary>
		/// Plays the current media
		/// </summary>
		void Play();

		/// <summary>
		/// Pauses the current media
		/// </summary>
		void Pause();

		/// <summary>
		/// Stops the current media
		/// </summary>
		void Stop();

		/// <summary>
		/// Initialize the source
		/// </summary>
		void InitializeSource();

		/// <summary>
		/// Toggles the mute state
		/// </summary>
		void ToggleMute();

		/// <summary>
		/// Notifies the extension that the volume has changed
		/// </summary>
		void OnVolumeChanged();

		/// <summary>
		/// Initializes the extension
		/// </summary>
		void Initialize();

		/// <summary>
		/// Notifies the extension that an option has changed
		/// </summary>
		void OnOptionChanged(string name, object value);

		/// <summary>
		/// Previous Track on a Playlist
		/// </summary>
		void PreviousTrack();

		/// <summary>
		/// Next Track on a Playlist
		/// </summary>
		void NextTrack();
	}
}
