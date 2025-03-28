#nullable enable

using Windows.UI.Core;
using LibVLCSharp.Shared;
using Windows.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml;
using Uno.Extensions;
using Uno.Logging;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Uno.Helpers;
using System.IO;

namespace Uno.UI.Media;

public partial class GtkMediaPlayer
{
	private static object s_lock = new();

	private Task? _initializationTask;
	private MediaPlayerElement? _mpe;
	private static ConditionalWeakTable<object, WeakReference<GtkMediaPlayer>> _playerMap = new();
	private static ConditionalWeakTable<object, WeakReference<GtkMediaPlayer>> _videoViewMap = new();
	private static ConditionalWeakTable<object, WeakReference<GtkMediaPlayer>> _mediaMap = new();

	public event EventHandler<object>? OnSourceFailed;
	public event EventHandler<object>? OnSourceEnded;
	public event EventHandler<object>? OnMetadataLoaded;
	public event EventHandler<object>? OnTimeUpdate;
	public event EventHandler<object>? OnSourceLoaded;
	public event Action? OnNaturalVideoDimensionChanged;

	private bool _updateVideoSizeOnFirstTimeStamp = true;
	private bool _isParsedLocalFile;

	const string MsAppXScheme = "ms-appx";

	private async Task Initialize()
	{
		if (_initializationTask is null)
		{
			_initializationTask = InitializeInner();
		}

		await _initializationTask;
	}

	private async Task InitializeInner()
	{
		await Task.Run(() =>
		{
			try
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Creating libvlc");
				}

				_libvlc = new LibVLC(enableDebugLogs: false);

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Creating player");
				}

				_mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libvlc);

				if (TryGetEventManagerProperty(_mediaPlayer, out var playerManagerProperty)
					&& playerManagerProperty.GetValue(_mediaPlayer) is { } eventManager)
				{
					_playerMap.Add(eventManager, new WeakReference<GtkMediaPlayer>(this));
				}
				else
				{
					throw new NotSupportedException("This version of libVLC is not supported (Missing EventManager property). Report this to the Uno Platform repository.");
				}

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Creating VideoView");
				}

				// This code path is executed on a ThreadPool thread.
				// So, when multiple media players are created, we will be executing this concurrently.
				// This causes an issue with GtkSharp type registration where it logs that VideoView type is already registered.
				// Note: Looking at GtkSharp code, it's using locks for type registration which implies the intent for that path is to be thread-safe.
				// But for some reason, this is buggy in GtkSharp and is not really thread-safe.
				// For now, we ensure thread-safety ourselves by locking here.
				lock (s_lock)
				{
					_videoView = new VideoView();
				}

				_videoViewMap.Add(_videoView, new WeakReference<GtkMediaPlayer>(this));

				_videoView.VideoSurfaceInteraction += static (s, e) =>
				{
					if (GetGtkPlayerForVideoView(s, out var target))
					{
						target.OnVideoViewVideoSurfaceInteraction(s, e);
					}
					else
					{
						if (typeof(GtkMediaPlayer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							typeof(GtkMediaPlayer).Log().Debug($"Unable to process interaction event, the GtkMediaPlayer instance cannot be found");
						}
					}
				};
			}
			catch (Exception ex)
			{
				if (typeof(GtkMediaPlayer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					typeof(GtkMediaPlayer).Log().Error($"Failed to initialize libVLC. See for more details: https://aka.platform.uno/mediaplayerelement", ex);
				}
			}
		});

		await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			_videoContainer = new ContentPresenter
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch,
			};

			if (_videoView != null && _mediaPlayer != null && _videoContainer != null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Set MediaPlayer");
				}

				_videoView.Visible = true;
				_videoView.MediaPlayer = _mediaPlayer;

				_mediaPlayer.TimeChanged += static (s, e) =>
				{
					if (GetGtkPlayerForVlcPlayer(s, out var target))
					{
						_ = target.Dispatcher.RunAsync(
							CoreDispatcherPriority.Normal,
							() => target.OnMediaPlayerTimeChange(s, e));
					}
				};

				_mediaPlayer.MediaChanged += static (s, e) =>
				{
					if (GetGtkPlayerForVlcPlayer(s, out var target))
					{
						_ = target.Dispatcher.RunAsync(
							CoreDispatcherPriority.Normal,
							() => target.OnMediaPlayerMediaChanged(s, e));
					}
				};

				_mediaPlayer.Stopped += static (s, e) =>
				{
					if (GetGtkPlayerForVlcPlayer(s, out var target))
					{
						_ = target.Dispatcher.RunAsync(
							CoreDispatcherPriority.Normal,
							() => target.OnMediaPlayerStopped(s, e));
					}
				};

				_mediaPlayer.Playing += static (s, e) =>
				{
					if (GetGtkPlayerForVlcPlayer(s, out var target))
					{
						_ = target.Dispatcher.RunAsync(
							CoreDispatcherPriority.Normal,
							() => target.OnMediaPlayerPlaying(s, e));
					}
				};

				_mediaPlayer.EncounteredError += static (s, e) =>
				{
					if (GetGtkPlayerForVlcPlayer(s, out var target))
					{
						_ = target.Dispatcher.RunAsync(
							CoreDispatcherPriority.Normal,
							() => target.OnMediaPlayerEncounteredError(s, e));
					}
				};

				_videoContainer.Content = _videoView;
				AddChild(_videoContainer);
				UpdateVideoStretch();

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Created player");
				}
			}

			UpdateMedia();
		});
	}

	private void OnMediaPlayerEncounteredError(object? s, EventArgs e)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"The native player encountered an error");
		}
	}

	private bool TryGetEventManagerProperty(
		object instance,
		[NotNullWhen(true)] out PropertyInfo? propertyInfo)
	{
		if (instance.GetType().GetProperty("EventManager", BindingFlags.NonPublic | BindingFlags.Instance) is { } info)
		{
			propertyInfo = info;
			return true;
		}
		propertyInfo = null;
		return false;
	}

	static bool GetGtkPlayerForVlcPlayer(
		object? instance,
		[NotNullWhen(true)] out GtkMediaPlayer? player)
	{
		if (instance is not null
			&& _playerMap.TryGetValue(instance, out var weakTarget)
			&& weakTarget.TryGetTarget(out player))
		{
			return true;
		}

		player = null;
		return false;
	}

	static bool GetGtkPlayerForVlcMedia(
		object? instance,
		[NotNullWhen(true)] out GtkMediaPlayer? player)
	{
		if (instance is not null
			&& _mediaMap.TryGetValue(instance, out var weakTarget)
			&& weakTarget.TryGetTarget(out player))
		{
			return true;
		}

		player = null;
		return false;
	}

	static bool GetGtkPlayerForVideoView(
		object? instance,
		[NotNullWhen(true)] out GtkMediaPlayer? player)
	{
		if (instance is not null
			&& _videoViewMap.TryGetValue(instance, out var weakTarget)
			&& weakTarget.TryGetTarget(out player))
		{
			return true;
		}

		player = null;
		return false;
	}

	private void OnVideoViewVideoSurfaceInteraction(object? sender, EventArgs e)
	{
		UpdateMediaPlayerElementReference();

		if (_mpe is not null)
		{
			_mpe.TransportControls.Show();
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Unable to find a MediaPlayerElement instance to show the transport controls");
			}
		}
	}

	private void UpdateMediaPlayerElementReference()
		=> _mpe ??= _owner.FindFirstParent<MediaPlayerElement>();

	private void OnSourceVideoLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = true;
		}

		UpdateVideoStretch();
		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
	}

	private static void OnSourceChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
	{
		if (source is GtkMediaPlayer player && args.NewValue is string encodedSource)
		{
			player.SetSource(encodedSource);
		}
		else
		{
			if (typeof(GtkMediaPlayer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				typeof(GtkMediaPlayer).Log().Error($"Invalid source [{args.NewValue}]");
			}
		}
	}

	private void SetSource(string encodedSource)
	{
		_updateVideoSizeOnFirstTimeStamp = true;
		_isParsedLocalFile = false;

		if (typeof(GtkMediaPlayer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			typeof(GtkMediaPlayer).Log().Debug($"Using source {encodedSource}");
		}

		if (Uri.TryCreate(encodedSource, UriKind.RelativeOrAbsolute, out var sourceUri))
		{
			if (!sourceUri.IsAbsoluteUri || sourceUri.Scheme == "")
			{
				sourceUri = new Uri(MsAppXScheme + ":///" + sourceUri.OriginalString.TrimStart('/'));
			}

			if (sourceUri.IsLocalResource())
			{
				var filePath = sourceUri.PathAndQuery;

				if (sourceUri.Host is { Length: > 0 } host)
				{
					filePath = host + "/" + filePath.TrimStart('/');
				}

				var originalLocalPath =
				Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath,
					 filePath.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar)
				);
				_isParsedLocalFile = true;
				_mediaPath = new Uri(originalLocalPath);
				UpdateMedia();
				return;
			}

			if (sourceUri.IsAppData())
			{
				var filePath = AppDataUriEvaluator.ToPath(sourceUri);
				_mediaPath = new Uri(filePath);
				_isParsedLocalFile = true;
				UpdateMedia();
				return;
			}

			if (sourceUri.IsFile)
			{
				_mediaPath = sourceUri;
				_isParsedLocalFile = true;
				UpdateMedia();
				return;
			}

			_mediaPath = sourceUri;
			UpdateMedia();
		}
		else
		{
			if (typeof(GtkMediaPlayer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				typeof(GtkMediaPlayer).Log().Error($"Unable to parse source [{encodedSource}]");
			}
		}
	}

	private void UpdateMedia()
	{
		if (_mediaPlayer != null && _libvlc != null && _mediaPath != null)
		{
			string[] options = new string[1];
			var media = new LibVLCSharp.Shared.Media(_libvlc, _mediaPath, options);

			media.Parse(_isParsedLocalFile ? MediaParseOptions.ParseLocal : MediaParseOptions.ParseNetwork);
			_mediaPlayer.Media = media;
			AddMediaEvents();
			Duration = (double)(_videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);
			OnSourceLoaded?.Invoke(this, EventArgs.Empty);

			UpdateVideoStretch();
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to update the media, the player is not ready yet");
			}
		}
	}

	private static void OnStaticDurationChanged(object? sender, EventArgs args)
	{
		if (GetGtkPlayerForVlcMedia(sender, out var target))
		{
			_ = target.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() => target.OnMediaDurationChanged(sender, args));
		}
		else
		{
			if (typeof(GtkMediaPlayer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				typeof(GtkMediaPlayer).Log().Error("OnStaticDurationChanged: Failed to find player instance for media");
			}
		}
	}

	private static void OnStaticMetaChanged(object? sender, EventArgs args)
	{
		if (GetGtkPlayerForVlcMedia(sender, out var target))
		{
			_ = target.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() => target.OnMediaMetaChanged(sender, args));
		}
		else
		{
			if (typeof(GtkMediaPlayer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				typeof(GtkMediaPlayer).Log().Error("OnStaticMetaChanged: Failed to find player instance for media");
			}
		}
	}

	private static void OnStaticStateChanged(object? sender, MediaStateChangedEventArgs args)
	{
		if (GetGtkPlayerForVlcMedia(sender, out var target))
		{
			_ = target.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() => target.OnMediaStateChanged(sender, args));
		}
		else
		{
			if (typeof(GtkMediaPlayer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				typeof(GtkMediaPlayer).Log().Error("OnStaticStateChanged: Failed to find player instance for media");
			}
		}
	}

	private static void OnStaticParsedChanged(object? sender, MediaParsedChangedEventArgs args)
	{
		if (GetGtkPlayerForVlcMedia(sender, out var target))
		{
			_ = target.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() => target.OnMediaParsedChanged(sender, args));
		}
		else
		{
			if (typeof(GtkMediaPlayer).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				typeof(GtkMediaPlayer).Log().Error("OnStaticParsedChanged: Failed to find player instance for media");
			}
		}
	}

	private void AddMediaEvents()
	{
		if (_mediaPlayer?.Media is { } media)
		{
			if (TryGetEventManagerProperty(media, out var managerProperty)
				&& managerProperty.GetValue(media) is { } eventManager)
			{
				_mediaMap.Add(eventManager, new WeakReference<GtkMediaPlayer>(this));
			}
			else
			{
				throw new NotSupportedException("This version of libVLC is not supported (Missing EventManager property). Report this to the Uno Platform repository.");
			}

			media.DurationChanged -= OnStaticDurationChanged;
			media.MetaChanged -= OnStaticMetaChanged;
			media.StateChanged -= OnStaticStateChanged;
			media.ParsedChanged -= OnStaticParsedChanged;

			media.DurationChanged += OnStaticDurationChanged;
			media.MetaChanged += OnStaticMetaChanged;
			media.StateChanged += OnStaticStateChanged;
			media.ParsedChanged += OnStaticParsedChanged;
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to add media events, the player is not ready yet");
			}
		}
	}

	private void OnMediaParsedChanged(object? sender, MediaParsedChangedEventArgs args)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnMediaParsedChanged: {args.ParsedStatus}");
		}

		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
		OnGtkSourceLoaded(sender, args);
	}

	private void OnMediaDurationChanged(object? sender, EventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnMediaDurationChanged (duration: {_videoView?.MediaPlayer?.Media?.Duration})");
		}

		Duration = (double)(_videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);
	}

	private void OnMediaStateChanged(object? sender, MediaStateChangedEventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnMediaStateChanged (state: {el.State})");
		}

		switch (el.State)
		{
			case VLCState.Opening:
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Opening");
				}
				OnGtkSourceLoaded(sender, el);
				break;

			case VLCState.Ended:
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Ended");
				}
				if (!_isEnding)
				{
					OnEndReached();
				}
				break;

			case VLCState.Error:
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Error");
				}
				OnGtkSourceFailed(sender, el);
				break;

			case VLCState.NothingSpecial:
			case VLCState.Buffering:
			case VLCState.Playing:
			case VLCState.Paused:
			case VLCState.Stopped:
				break;

			default:
				break;
		}
	}

	private void OnMediaMetaChanged(object? sender, EventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnMediaMetaChanged");
		}

		OnGtkMetadataLoaded(sender, el);
	}

	private void OnMediaPlayerStopped(object? sender, EventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"MediaPlayer Stopped");
		}

		if (_videoView != null)
		{
			if (!_isEnding)
			{
				_videoView.Visible = false;
			}
		}
	}

	private void OnMediaPlayerMediaChanged(object? sender, MediaPlayerMediaChangedEventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnMediaPlayerMediaChanged");
		}

		OnGtkSourceLoaded(sender, el);
	}

	private void OnMediaPlayerPlaying(object? s, EventArgs e)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnMediaPlayerPlaying");
		}

		UpdateVideoStretch(forceVideoViewVisibility: true);
	}

	private void OnMediaPlayerTimeChange(object? sender, MediaPlayerTimeChangedEventArgs el)
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnMediaPlayerTimeChange ({el.Time})");
		}

		var time = el is LibVLCSharp.Shared.MediaPlayerTimeChangedEventArgs e
			? TimeSpan.FromMilliseconds(e.Time)
			: TimeSpan.Zero;

		OnTimeUpdate?.Invoke(this, time);

		if (_updateVideoSizeOnFirstTimeStamp
			&& TryGetVideoDetails(out _, out _, out _))
		{
			// Actual media information may not be available until the
			// first time stamp is received. Once we do, we can update the
			// video's render size.

			_updateVideoSizeOnFirstTimeStamp = false;

			UpdateVideoStretch(forceVideoViewVisibility: true);
		}
	}

	private void OnEndReached()
	{
		if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnEndReached");
		}

		if (_videoView != null && _mediaPlayer != null)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				_isEnding = true;
				UpdateMedia();

				OnSourceEnded?.Invoke(this, EventArgs.Empty);
				_videoView.Visible = true;
				_isEnding = false;
			});
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to process end reched, the player is not ready yet");
			}
		}
	}

	private void OnGtkMetadataLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null && _mediaPlayer != null && _mediaPlayer.Media != null)
		{
			Duration = (double)_mediaPlayer.Media.Duration / 1000;
			UpdateVideoStretch();

			OnMetadataLoaded?.Invoke(this, Duration);
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to process metadata loaded, the player is not ready yet");
			}
		}
	}

	private void OnGtkSourceLoaded(object? sender, EventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = true;
			UpdateVideoStretch();

			Duration = (double)(_videoView?.MediaPlayer?.Media?.Duration / 1000 ?? 0);

			if (Duration > 0)
			{
				OnSourceLoaded?.Invoke(this, EventArgs.Empty);
			}
		}
		else
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("Unable to process source loaded, the player is not ready yet");
			}
		}
	}

	private void OnGtkSourceFailed(object? sender, MediaStateChangedEventArgs e)
	{
		if (_videoView != null)
		{
			_videoView.Visible = false;
		}

		OnSourceFailed?.Invoke(this, e.State.ToString());
	}
}
