using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Uno.UI.Media;

internal partial class HtmlMediaPlayer : Border
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
		_htmlVideo.SetCssStyle("visibility", "hidden");
		_htmlAudio.SetCssStyle("visibility", "hidden");

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Adding media elements");
		}

		AddChild(_htmlVideo);
		AddChild(_htmlAudio);

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}


	private void OnLoaded(object sender, object args)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"HtmlMediaPlayer Loaded");
		}

		SourceLoaded += OnHtmlSourceLoaded;
		SourceFailed += OnHtmlSourceFailed;
		SourceEnded += OnHtmlSourceEnded;
		MetadataLoaded += OnHtmlMetadataLoaded;
		TimeUpdated += OnHtmlTimeUpdated;
	}

	private void OnUnloaded(object sender, object args)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"HtmlMediaPlayer Unloaded");
		}

		SourceLoaded -= OnHtmlSourceLoaded;
		SourceFailed -= OnHtmlSourceFailed;
		SourceEnded -= OnHtmlSourceEnded;
		MetadataLoaded -= OnHtmlMetadataLoaded;
		TimeUpdated -= OnHtmlTimeUpdated;
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
	event EventHandler TimeUpdated
	{
		add
		{
			_htmlVideo.RegisterHtmlEventHandler("timeupdate", value);
			_htmlAudio.RegisterHtmlEventHandler("timeupdate", value);
		}
		remove
		{
			_htmlVideo.UnregisterHtmlEventHandler("timeupdate", value);
			_htmlAudio.UnregisterHtmlEventHandler("timeupdate", value);
		}
	}

	/// <summary>
	/// Occurs when metadata for the specified audio/video has been loaded.
	/// </summary>
	event EventHandler MetadataLoaded
	{
		add
		{
			_htmlVideo.RegisterHtmlEventHandler("loadedmetadata", value);
			_htmlAudio.RegisterHtmlEventHandler("loadedmetadata", value);
		}
		remove
		{
			_htmlVideo.UnregisterHtmlEventHandler("loadedmetadata", value);
			_htmlAudio.UnregisterHtmlEventHandler("loadedmetadata", value);
		}
	}

	/// <summary>
	/// Occurs when the video source has ended playing.
	/// </summary>
	event EventHandler SourceEnded
	{
		add
		{
			_htmlVideo.RegisterHtmlEventHandler("ended", value);
			_htmlAudio.RegisterHtmlEventHandler("ended", value);
		}
		remove
		{
			_htmlVideo.UnregisterHtmlEventHandler("ended", value);
			_htmlAudio.UnregisterHtmlEventHandler("ended", value);
		}
	}

	/// <summary>
	/// Occurs when the video source is downloaded and decoded with no
	/// failure. You can use this event to determine the natural size
	/// of the image source.
	/// </summary>
	event EventHandler SourceLoaded
	{
		add
		{
			_htmlVideo.RegisterHtmlEventHandler("loadeddata", value);
			_htmlAudio.RegisterHtmlEventHandler("loadeddata", value);
		}
		remove
		{
			_htmlVideo.UnregisterHtmlEventHandler("loadeddata", value);
			_htmlAudio.UnregisterHtmlEventHandler("loadeddata", value);
		}
	}

	/// <summary>
	/// Occurs when there is an error associated with video retrieval or format.
	/// </summary>		
	event EventHandler<HtmlCustomEventArgs> SourceFailed
	{
		add
		{
			_htmlVideo.RegisterHtmlCustomEventHandler("error", value, isDetailJson: false);
			_htmlAudio.RegisterHtmlCustomEventHandler("error", value, isDetailJson: false);
		}
		remove
		{
			_htmlVideo.RegisterHtmlCustomEventHandler("error", value);
			_htmlAudio.RegisterHtmlCustomEventHandler("error", value);
		}
	}

	private void OnHtmlTimeUpdated(object sender, EventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Time updated [{Source}]");
		}

		OnTimeUpdate?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlSourceEnded(object sender, EventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Media ended [{Source}]");
		}

		OnSourceEnded?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlMetadataLoaded(object sender, EventArgs e)
	{
		Duration = NativeMethods.GetDuration(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId);
		OnMetadataLoaded?.Invoke(this, Duration);
	}

	private void OnHtmlSourceLoaded(object sender, EventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Media opened [{Source}]");
		}

		if (IsVideo)
		{
			_htmlVideo.SetCssStyle("visibility", "visible");
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Video source loaded: [{Source}]");
			}
		}
		else if (IsAudio)
		{
			_htmlAudio.SetCssStyle("visibility", "visible");
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Audio source loaded: [{Source}]");
			}
		}

		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlSourceFailed(object sender, HtmlCustomEventArgs e)
	{
		if (IsVideo)
		{
			_htmlVideo.SetCssStyle("visibility", "hidden");
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Error($"Video source failed: [{Source}]");
			}
		}
		else if (IsAudio)
		{
			_htmlAudio.SetCssStyle("visibility", "hidden");
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Error($"Audio source failed: [{Source}]");
			}
		}

		OnSourceFailed?.Invoke(this, e.Detail);
	}

	public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
		"Source", typeof(string), typeof(HtmlMediaPlayer), new PropertyMetadata(default(string),
			OnSourceChanged));

	private static void OnSourceChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is HtmlMediaPlayer player)
		{
			var encodedSource = WebAssemblyRuntime.EscapeJs((string)args.NewValue);

			if (player.Log().IsEnabled(LogLevel.Debug))
			{
				player.Log().Debug($"HtmlMediaPlayer.OnSourceChanged: {args.NewValue} isVideo:{player.IsVideo} isAudio:{player.IsAudio}");
			}

			if (player.IsVideo)
			{
				_htmlVideo.SetHtmlAttribute("src", encodedSource);
				if (player.Log().IsEnabled(LogLevel.Debug))
				{
					player.Log().Debug($"Video source changed: [{player.Source}]");
				}
			}
			else if (player.IsAudio)
			{
				_htmlAudio.SetHtmlAttribute("src", encodedSource);
				if (player.Log().IsEnabled(LogLevel.Debug))
				{
					player.Log().Debug($"Audio source changed: [{player.Source}]");
				}
			}
			else
			{
				if (player.Log().IsEnabled(LogLevel.Debug))
				{
					player.Log().Debug($"HtmlMediaPlayer.OnSourceChanged: unsupported source");
				}
			}
		}
	}

	public static DependencyProperty AutoPlayProperty { get; } = DependencyProperty.Register(
	nameof(AutoPlay), typeof(bool), typeof(HtmlMediaPlayer), new PropertyMetadata(false,
		OnAutoPlayChanged));

	private static void OnAutoPlayChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is HtmlMediaPlayer player)
		{
			NativeMethods.SetAutoPlay(player.IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId, (bool)args.NewValue);
		}
	}

	public static DependencyProperty AreTransportControlsEnabledProperty { get; } = DependencyProperty.Register(
		"AreTransportControlsEnabled", typeof(bool), typeof(HtmlMediaPlayer), new PropertyMetadata(true,
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
					if (!string.IsNullOrEmpty(_htmlVideo.GetHtmlAttribute("controls")))
					{
						_htmlVideo.SetHtmlAttribute("controls", "");
					}
					else
					{
						_htmlVideo.SetHtmlAttribute("controls", "controls");
					}
				}
				else if (player.IsAudio)
				{
					if (!string.IsNullOrEmpty(_htmlAudio.GetHtmlAttribute("controls")))
					{
						_htmlAudio.SetHtmlAttribute("controls", "");

					}
					else
					{
						_htmlAudio.SetHtmlAttribute("controls", "controls");
					}
				}
			}
			else
			{
				if (player.IsVideo)
				{
					if (!string.IsNullOrEmpty(_htmlVideo.GetHtmlAttribute("controls")))
					{
						_htmlVideo.SetHtmlAttribute("controls", "");
					}
				}
				else if (player.IsAudio)
				{
					if (!string.IsNullOrEmpty(_htmlAudio.GetHtmlAttribute("controls")))
					{
						_htmlAudio.SetHtmlAttribute("controls", "");
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
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"RequestFullScreen()");
		}

		NativeMethods.RequestFullScreen(_htmlVideo.HtmlId);
	}

	public void ExitFullScreen()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"ExitFullScreen()");
		}

		NativeMethods.ExitFullScreen();
	}

	public void Pause()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Pause()");
		}

		NativeMethods.Pause(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId);
	}

	internal void UpdateVideoStretch(VideoStretch stretch)
	{
		switch (stretch)
		{
			case VideoStretch.None:
				_htmlVideo.SetCssStyle("object-fit", "none");
				break;
			case VideoStretch.Fill:
				_htmlVideo.SetCssStyle("object-fit", "fill");
				break;
			case VideoStretch.Uniform:
				_htmlVideo.SetCssStyle("object-fit", "cover");
				break;
			case VideoStretch.UniformToFill:
				_htmlVideo.SetCssStyle("object-fit", "contain");
				break;
		}
	}

	public void Play()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Play()");
		}

		NativeMethods.Play(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId);
	}

	public void Stop()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Stop()");
		}

		NativeMethods.Stop(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId);
	}
}
