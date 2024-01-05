#if !(__ANDROID__ || __IOS__ || __MACOS__)
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Uno.Media.Playback;
using Windows.ApplicationModel.Email;
using System.Drawing;

namespace Windows.Media.Playback
{
	public partial class MediaPlayer
	{
		private IMediaPlayerExtension? _extension;

		public void Initialize()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("Enter Initialize MediaPlayer().");
			}

			if (!ApiExtensibility.CreateInstance<IMediaPlayerExtension>(this, out _extension))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("The MediaPlayer extension is not installed. For more information aka.platform.uno/mediaplayerelement");
				}

				return;
			}

			_extension.Events = new MediaPlayerEvents(this);
		}

		partial void OnOptionChanged(string name, object value)
			=> _extension?.OnOptionChanged(name, value);

		partial void OnTransportControlBoundsChanged(Rect bounds)
			=> _extension?.SetTransportControlsBounds(bounds);

		public void Dispose()
			=> _extension?.Dispose();

		public double PlaybackRate
		{
			get => _extension?.PlaybackRate ?? 0.0;
			set
			{
				if (_extension is not null)
				{
					_extension.PlaybackRate = value;
				}
			}
		}

		public bool IsLoopingEnabled
		{
			get => _extension?.IsLoopingEnabled ?? false;
			set
			{
				if (_extension is not null)
				{
					_extension.IsLoopingEnabled = value;
				}
			}
		}

		public bool IsLoopingAllEnabled
		{
			get => _extension?.IsLoopingAllEnabled ?? false;
			set
			{
				if (_extension is not null)
				{
					_extension.IsLoopingAllEnabled = value;
				}
			}
		}
		public MediaPlayerState CurrentState
			=> _extension?.CurrentState ?? MediaPlayerState.Closed;

		public TimeSpan NaturalDuration
			=> _extension?.NaturalDuration ?? TimeSpan.Zero;

		public bool IsProtected
			=> _extension?.IsProtected ?? false;

		public double BufferingProgress
			=> _extension?.BufferingProgress ?? 0.0;

		public bool CanPause
			=> _extension?.CanPause ?? false;

		public bool CanSeek
			=> _extension?.CanSeek ?? false;

		public MediaPlayerAudioDeviceType AudioDeviceType
		{
			get => _extension?.AudioDeviceType ?? MediaPlayerAudioDeviceType.Multimedia;
			set
			{
				if (_extension is not null)
				{
					_extension.AudioDeviceType = value;
				}
			}
		}

		public MediaPlayerAudioCategory AudioCategory
		{
			get => _extension?.AudioCategory ?? MediaPlayerAudioCategory.Other;
			set
			{
				if (_extension is not null)
				{
					_extension.AudioCategory = value;
				}
			}
		}

		public TimeSpan TimelineControllerPositionOffset
		{
			get => _extension?.TimelineControllerPositionOffset ?? TimeSpan.Zero;
			set
			{
				if (_extension is not null)
				{
					_extension.TimelineControllerPositionOffset = value;
				}
			}
		}

		public bool RealTimePlayback
		{
			get => _extension?.RealTimePlayback ?? false;
			set
			{
				if (_extension is not null)
				{
					_extension.RealTimePlayback = value;
				}
			}
		}

		public double AudioBalance
		{
			get => _extension?.AudioBalance ?? 0.0;
			set
			{
				if (_extension is not null)
				{
					_extension.AudioBalance = value;
				}
			}
		}

		public TimeSpan Position
		{
			get => _extension?.Position ?? TimeSpan.Zero;
			set
			{
				if (_extension is not null)
				{
					_extension.Position = value;
				}
			}
		}

		public bool IsVideo => _extension?.IsVideo ?? false;

		public void SetUriSource(global::System.Uri value)
			=> _extension?.SetUriSource(value);

		public void SetFileSource(global::Windows.Storage.IStorageFile file)
			=> _extension?.SetFileSource(file);

		public void SetStreamSource(global::Windows.Storage.Streams.IRandomAccessStream stream)
			=> _extension?.SetStreamSource(stream);

		public void SetMediaSource(global::Windows.Media.Core.IMediaSource source)
			=> _extension?.SetMediaSource(source);

		public void StepForwardOneFrame()
			=> _extension?.StepForwardOneFrame();

		public void StepBackwardOneFrame()
			=> _extension?.StepBackwardOneFrame();

		public void SetSurfaceSize(global::Windows.Foundation.Size size)
			=> _extension?.SetSurfaceSize(size);

		public void Play()
			=> _extension?.Play();

		public void Pause()
			=> _extension?.Pause();

		public void Stop()
			=> _extension?.Stop();

		public void InitializeSource()
			=> _extension?.InitializeSource();

		public void ToggleMute()
			=> _extension?.ToggleMute();

		public void OnVolumeChanged()
			=> _extension?.OnVolumeChanged();

		public void PreviousTrack()
			=> _extension?.PreviousTrack();
		public void NextTrack()
			=> _extension?.NextTrack();

		public event TypedEventHandler<MediaPlayer, object>? BufferingEnded;
		public event TypedEventHandler<MediaPlayer, object>? BufferingStarted;
		public event TypedEventHandler<MediaPlayer, object>? CurrentStateChanged;
		public event TypedEventHandler<MediaPlayer, MediaPlayerRateChangedEventArgs>? MediaPlayerRateChanged;
		public event TypedEventHandler<MediaPlayer, PlaybackMediaMarkerReachedEventArgs>? PlaybackMediaMarkerReached;
		public event TypedEventHandler<MediaPlayer, object>? VideoFrameAvailable;
		public event TypedEventHandler<MediaPlayer, object>? SubtitleFrameChanged;
	}
}
#endif
