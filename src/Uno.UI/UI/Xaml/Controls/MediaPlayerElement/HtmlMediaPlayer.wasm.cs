using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Media.Playback;
using NativeMethods = __Windows.Devices.Media.MediaElement.NativeMethods;

namespace Windows.UI.Xaml.Controls;

internal class HtmlMediaPlayer : Border, IHtmlMediaPlayer
{
	private static readonly HtmlVideo _htmlVideo = new HtmlVideo();
	private static readonly HtmlAudio _htmlAudio = new HtmlAudio();
	private readonly ImmutableArray<string> audioTagAllowedFormats =
		ImmutableArray.Create(new string[] { ".MP3", ".WAV" });
	private readonly ImmutableArray<string> videoTagAllowedFormats =
		ImmutableArray.Create(new string[] { ".MP4", ".WEBM", ".OGG" });

	public event EventHandler<object> OnSourceLoaded;
	public event EventHandler<object> OnSourceFailed;
	public event EventHandler<object> OnSourceEnded;
	public event EventHandler<object> OnMetadataLoaded;
	public event EventHandler<object> OnTimeUpdate;

	public HtmlMediaPlayer()
	{
		_htmlVideo.SetStyle("visibility", "hidden");
		_htmlAudio.SetStyle("visibility", "hidden");

		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug("Adding media elements");
		}

		AddChild(_htmlVideo);
		AddChild(_htmlAudio);
	}

	public bool IsAudio
	{
		get => audioTagAllowedFormats.Contains(Path.GetExtension(Source), StringComparer.OrdinalIgnoreCase);
	}

	public bool IsVideo
	{
		get => videoTagAllowedFormats.Contains(Path.GetExtension(Source), StringComparer.OrdinalIgnoreCase);
	}

	public int VideoWidth
	{
		get => NativeMethods.VideoWidth(_htmlVideo.HtmlId);
	}

	public int VideoHeight
	{
		get => NativeMethods.VideoHeight(_htmlVideo.HtmlId);
	}

	/// <summary>
	/// Gets/sets current player position in seconds
	/// </summary>
	public double CurrentPosition
	{
		get => NativeMethods.GetCurrentPosition(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId);
		set => NativeMethods.SetCurrentPosition(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId, value);
	}

	public double Duration { get; set; }

	public void SetAnonymousCORS(bool enable)
	{
		_htmlAudio.SetAnonymousCORS(enable);
		_htmlVideo.SetAnonymousCORS(enable);

		NativeMethods.Reload(_htmlAudio.HtmlId);
		NativeMethods.Reload(_htmlVideo.HtmlId);
	}

	public void SetVolume(float volume)
	{
		NativeMethods.SetVolume(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId, volume);
	}

	/// <summary>
	/// Occurs when the playing position of an audio/video has changed.
	/// </summary>
	event RoutedEventHandler TimeUpdated
	{
		add
		{
			_htmlVideo.RegisterEventHandler("timeupdate", value, GenericEventHandlers.RaiseRoutedEventHandler);
			_htmlAudio.RegisterEventHandler("timeupdate", value, GenericEventHandlers.RaiseRoutedEventHandler);
		}
		remove
		{
			_htmlVideo.UnregisterEventHandler("timeupdate", value, GenericEventHandlers.RaiseRoutedEventHandler);
			_htmlAudio.UnregisterEventHandler("timeupdate", value, GenericEventHandlers.RaiseRoutedEventHandler);
		}
	}

	/// <summary>
	/// Occurs when metadata for the specified audio/video has been loaded.
	/// </summary>
	event RoutedEventHandler MetadataLoaded
	{
		add
		{
			_htmlVideo.RegisterEventHandler("loadedmetadata", value, GenericEventHandlers.RaiseRoutedEventHandler);
			_htmlAudio.RegisterEventHandler("loadedmetadata", value, GenericEventHandlers.RaiseRoutedEventHandler);
		}
		remove
		{
			_htmlVideo.UnregisterEventHandler("loadedmetadata", value, GenericEventHandlers.RaiseRoutedEventHandler);
			_htmlAudio.UnregisterEventHandler("loadedmetadata", value, GenericEventHandlers.RaiseRoutedEventHandler);
		}
	}

	/// <summary>
	/// Occurs when the video source has ended playing.
	/// </summary>
	event RoutedEventHandler SourceEnded
	{
		add
		{
			_htmlVideo.RegisterEventHandler("ended", value, GenericEventHandlers.RaiseRoutedEventHandler);
			_htmlAudio.RegisterEventHandler("ended", value, GenericEventHandlers.RaiseRoutedEventHandler);
		}
		remove
		{
			_htmlVideo.UnregisterEventHandler("ended", value, GenericEventHandlers.RaiseRoutedEventHandler);
			_htmlAudio.UnregisterEventHandler("ended", value, GenericEventHandlers.RaiseRoutedEventHandler);
		}
	}

	/// <summary>
	/// Occurs when the video source is downloaded and decoded with no failure. You can use this event to determine the natural size of the image source.
	/// </summary>
	event RoutedEventHandler SourceLoaded
	{
		add
		{
			_htmlVideo.RegisterEventHandler("loadeddata", value, GenericEventHandlers.RaiseRoutedEventHandler);
			_htmlAudio.RegisterEventHandler("loadeddata", value, GenericEventHandlers.RaiseRoutedEventHandler);
		}
		remove
		{
			_htmlVideo.UnregisterEventHandler("loadeddata", value, GenericEventHandlers.RaiseRoutedEventHandler);
			_htmlAudio.UnregisterEventHandler("loadeddata", value, GenericEventHandlers.RaiseRoutedEventHandler);
		}
	}

	/// <summary>
	/// Occurs when there is an error associated with video retrieval or format.
	/// </summary>		
	event ExceptionRoutedEventHandler SourceFailed
	{
		add
		{
			_htmlVideo.RegisterEventHandler("error", value, GenericEventHandlers.RaiseExceptionRoutedEventHandler, payloadConverter: SourceFailedConverter);
			_htmlAudio.RegisterEventHandler("error", value, GenericEventHandlers.RaiseExceptionRoutedEventHandler, payloadConverter: SourceFailedConverter);
		}
		remove
		{
			_htmlVideo.UnregisterEventHandler("error", value, GenericEventHandlers.RaiseExceptionRoutedEventHandler);
			_htmlAudio.UnregisterEventHandler("error", value, GenericEventHandlers.RaiseExceptionRoutedEventHandler);
		}
	}

	private ExceptionRoutedEventArgs SourceFailedConverter(object sender, string e)
			=> new ExceptionRoutedEventArgs(sender, e);

	private void OnHtmlTimeUpdated(object sender, RoutedEventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Time updated [{Source}]");
		}

		OnTimeUpdate?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlSourceEnded(object sender, RoutedEventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Media ended [{Source}]");
		}

		OnSourceEnded?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlMetadataLoaded(object sender, RoutedEventArgs e)
	{
		Duration = NativeMethods.GetDuration(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId);
		OnMetadataLoaded?.Invoke(this, Duration);
	}

	private void OnHtmlSourceLoaded(object sender, RoutedEventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Media opened [{Source}]");
		}

		if (IsVideo)
		{
			_htmlVideo.SetStyle("visibility", "visible");
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Video source loaded: [{Source}]");
			}
		}
		else if (IsAudio)
		{
			_htmlAudio.SetStyle("visibility", "visible");
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Audio source loaded: [{Source}]");
			}
		}

		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlSourceFailed(object sender, ExceptionRoutedEventArgs e)
	{
		if (IsVideo)
		{
			_htmlVideo.SetStyle("visibility", "hidden");
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Error($"Video source failed: [{Source}]");
			}
		}
		else if (IsAudio)
		{
			_htmlAudio.SetStyle("visibility", "hidden");
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Error($"Audio source failed: [{Source}]");
			}
		}

		OnSourceFailed?.Invoke(this, e.ErrorMessage);
	}

	public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
		"Source", typeof(string), typeof(HtmlMediaPlayer), new FrameworkPropertyMetadata(default(string),
			OnSourceChanged));

	private static void OnSourceChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is HtmlMediaPlayer player)
		{
			var encodedSource = WebAssemblyRuntime.EscapeJs((string)args.NewValue);

			if (player.IsVideo)
			{
				_htmlVideo.SetAttribute("src", encodedSource);
				if (player.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					player.Log().Debug($"Video source changed: [{player.Source}]");
				}
			}
			else if (player.IsAudio)
			{
				_htmlAudio.SetAttribute("src", encodedSource);
				if (player.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					player.Log().Debug($"Audio source changed: [{player.Source}]");
				}
			}
		}
	}
	public static DependencyProperty AutoPlayProperty { get; } = DependencyProperty.Register(
	nameof(AutoPlay), typeof(bool), typeof(HtmlMediaPlayer), new FrameworkPropertyMetadata(false,
		OnAutoPlayChanged));
	private static void OnAutoPlayChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is HtmlMediaPlayer player)
		{
			NativeMethods.SetAutoPlay(player.IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId, (bool)args.NewValue);
		}
	}
	public static DependencyProperty AreTransportControlsEnabledProperty { get; } = DependencyProperty.Register(
		"AreTransportControlsEnabled", typeof(bool), typeof(HtmlMediaPlayer), new FrameworkPropertyMetadata(true,
			OnAreTransportControlsEnabledChanged));
	private static void OnAreTransportControlsEnabledChanged(DependencyObject
		dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		var enabled = (bool)args.NewValue;

		if (dependencyobject is HtmlMediaPlayer player)
		{
			if (enabled)
			{
				if (player.IsVideo)
				{
					if (!string.IsNullOrEmpty(_htmlVideo.GetAttribute("controls")))
					{
						_htmlVideo.RemoveAttribute("controls");
					}
					else
					{
						_htmlVideo.SetAttribute("controls", "controls");
					}
				}
				else if (player.IsAudio)
				{
					if (!string.IsNullOrEmpty(_htmlAudio.GetAttribute("controls")))
					{
						_htmlAudio.RemoveAttribute("controls");

					}
					else
					{
						_htmlAudio.SetAttribute("controls", "controls");
					}
				}
			}
			else
			{
				if (player.IsVideo)
				{
					if (!string.IsNullOrEmpty(_htmlVideo.GetAttribute("controls")))
					{
						_htmlVideo.RemoveAttribute("controls");
					}
				}
				else if (player.IsAudio)
				{
					if (!string.IsNullOrEmpty(_htmlAudio.GetAttribute("controls")))
					{
						_htmlAudio.RemoveAttribute("controls");
					}
				}
			}
		}
	}

	public string Source
	{
		get => (string)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	public bool AutoPlay
	{
		get => (bool)GetValue(AutoPlayProperty);
		set => SetValue(AutoPlayProperty, value);
	}

	public bool AreTransportControlsEnabled
	{
		get => (bool)GetValue(AreTransportControlsEnabledProperty);
		set => SetValue(AreTransportControlsEnabledProperty, value);
	}

	public void RequestFullScreen()
	{
		NativeMethods.RequestFullScreen(_htmlVideo.HtmlId);
	}

	public void ExitFullScreen()
	{
		NativeMethods.ExitFullScreen();
	}

	public void Pause()
	{
		NativeMethods.Pause(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId);
	}

	public void UpdateVideoStretch(MediaPlayer.VideoStretch stretch)
	{
		switch (stretch)
		{
			case MediaPlayer.VideoStretch.None:
				_htmlVideo.SetStyle("object-fit", "none");
				break;
			case MediaPlayer.VideoStretch.Fill:
				_htmlVideo.SetStyle("object-fit", "fill");
				break;
			case MediaPlayer.VideoStretch.Uniform:
				_htmlVideo.SetStyle("object-fit", "cover");
				break;
			case MediaPlayer.VideoStretch.UniformToFill:
				_htmlVideo.SetStyle("object-fit", "contain");
				break;
		}
	}

	public void Play()
	{
		NativeMethods.Play(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId);
	}

	public void Stop()
	{
		NativeMethods.Stop(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId);
	}

	private protected override void OnLoaded()
	{
		base.OnLoaded();
		SourceLoaded += OnHtmlSourceLoaded;
		SourceFailed += OnHtmlSourceFailed;
		SourceEnded += OnHtmlSourceEnded;
		MetadataLoaded += OnHtmlMetadataLoaded;
		TimeUpdated += OnHtmlTimeUpdated;
	}

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();
		SourceLoaded -= OnHtmlSourceLoaded;
		SourceFailed -= OnHtmlSourceFailed;
		SourceEnded -= OnHtmlSourceEnded;
		MetadataLoaded -= OnHtmlMetadataLoaded;
		TimeUpdated -= OnHtmlTimeUpdated;
	}
}
