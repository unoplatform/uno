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
using System.Globalization;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Media;

internal partial class HtmlMediaPlayer : Border
{
	private HtmlVideo _htmlVideo = new HtmlVideo();
	private HtmlAudio _htmlAudio = new HtmlAudio();
	private bool _isPlaying;

	private readonly ImmutableArray<string> audioTagAllowedFormats =
		ImmutableArray.Create(new string[] { ".MP3", ".WAV" });
	private readonly ImmutableArray<string> videoTagAllowedFormats =
		ImmutableArray.Create(new string[] { ".MP4", ".WEBM", ".OGG" });
	private UIElement ActiveElement;
	private string ActiveElementName;

	public event EventHandler<object> OnSourceLoaded;
	public event EventHandler<object> OnSourceFailed;
	public event EventHandler<object> OnSourceEnded;
	public event EventHandler<object> OnMetadataLoaded;
	public event EventHandler<object> OnTimeUpdate;

	public HtmlMediaPlayer()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Adding media elements");
		}
		_htmlVideo.SetCssStyle("visibility", "hidden");
		_htmlAudio.SetCssStyle("visibility", "hidden");

		AddChild(_htmlVideo);
		AddChild(_htmlAudio);

		ActiveElement = IsVideo ? _htmlVideo : IsAudio ? _htmlAudio : default;
		ActiveElementName = IsVideo ? "Video" : IsAudio ? "Audio" : "";

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}


	private void OnLoaded(object sender, object args)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"HtmlMediaPlayer Loaded");
		}
		ActiveElement = IsVideo ? _htmlVideo : IsAudio ? _htmlAudio : default;
		ActiveElementName = IsVideo ? "Video" : IsAudio ? "Audio" : "";
		SourceLoaded += OnHtmlSourceLoaded;
		SourceFailed += OnHtmlSourceFailed;
		SourceEnded += OnHtmlSourceEnded;
		MetadataLoaded += OnHtmlMetadataLoaded;

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
		get
		{
			if (ActiveElement == null)
			{
				return 0;
			}
			return NativeMethods.GetCurrentPosition(ActiveElement.HtmlId);
		}
		set
		{
			if (ActiveElement != null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"{ActiveElementName} on ID {ActiveElement.HtmlId} SetCurrentPosition : [{value}]");
				}
				NativeMethods.SetCurrentPosition(ActiveElement.HtmlId, value);
			}
		}
	}

	public double Duration { get; set; }

	public void SetAnonymousCORS(bool enable)
	{
		if (enable)
		{
			_htmlVideo.SetHtmlAttribute("crossorigin", "anonymous");
			_htmlAudio.SetHtmlAttribute("crossorigin", "anonymous");
		}
		else
		{
			if (!string.IsNullOrEmpty(_htmlVideo.GetHtmlAttribute("crossorigin")))
			{
				_htmlVideo.RemoveAttribute("crossorigin");
			}
			if (!string.IsNullOrEmpty(_htmlAudio.GetHtmlAttribute("crossorigin")))
			{
				_htmlAudio.RemoveAttribute("crossorigin");
			}
		}
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
			if (ActiveElement == null)
			{
				return;
			}
			ActiveElement.RegisterHtmlEventHandler("timeupdate", value);
		}
		remove
		{
			if (ActiveElement != null)
			{
				ActiveElement.UnregisterHtmlEventHandler("timeupdate", value);
			}
		}
	}

	/// <summary>
	/// Occurs when metadata for the specified audio/video has been loaded.
	/// </summary>
	event EventHandler MetadataLoaded
	{
		add
		{
			if (ActiveElement == null)
			{
				return;
			}
			ActiveElement.RegisterHtmlEventHandler("loadedmetadata", value);
		}
		remove
		{
			if (ActiveElement != null)
			{
				ActiveElement.UnregisterHtmlEventHandler("loadedmetadata", value);
			}
		}
	}

	/// <summary>
	/// Occurs when the video source has ended playing.
	/// </summary>
	event EventHandler SourceEnded
	{
		add
		{
			if (ActiveElement == null)
			{
				return;
			}
			ActiveElement.RegisterHtmlEventHandler("ended", value);
		}
		remove
		{
			if (ActiveElement != null)
			{
				ActiveElement.UnregisterHtmlEventHandler("ended", value);
			}
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
			_htmlVideo.UnregisterHtmlCustomEventHandler("error", value);
			_htmlAudio.UnregisterHtmlCustomEventHandler("error", value);
		}
	}

	private void OnHtmlTimeUpdated(object sender, EventArgs e)
	{
		//if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		//{
		//	this.Log().Debug($"Time updated [{Source}]");
		//}

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
		if (ActiveElement != null)
		{
			Duration = NativeMethods.GetDuration(ActiveElement.HtmlId);
		}
		OnMetadataLoaded?.Invoke(this, Duration);
	}

	private void OnHtmlSourceLoaded(object sender, EventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Media opened [{Source}]");
		}

		ActiveElement = IsVideo ? _htmlVideo : IsAudio ? _htmlAudio : default;
		ActiveElementName = IsVideo ? "Video" : IsAudio ? "Audio" : "";
		if (ActiveElement != null)
		{
			ActiveElement.SetCssStyle("visibility", "visible");
		}
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"{ActiveElementName} source loaded: [{Source}]");
		}

		TimeUpdated += OnHtmlTimeUpdated;
		if (ActiveElement != null)
		{
			Duration = NativeMethods.GetDuration(ActiveElement.HtmlId);
		}
		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlSourceFailed(object sender, HtmlCustomEventArgs e)
	{
		TimeUpdated += OnHtmlTimeUpdated;
		if (ActiveElement != null)
		{
			ActiveElement.SetCssStyle("visibility", "hidden");
		}
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Error($"{ActiveElementName} source failed: [{Source}]");
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

			player.TimeUpdated -= player.OnHtmlTimeUpdated;

			var encodedSource = WebAssemblyRuntime.EscapeJs((string)args.NewValue);

			if (player.Log().IsEnabled(LogLevel.Debug))
			{
				player.Log().Debug($"HtmlMediaPlayer.OnSourceChanged: {args.NewValue} isVideo:{player.IsVideo} isAudio:{player.IsAudio}");
			}

			player.ActiveElement = player.IsVideo ? player._htmlVideo : player.IsAudio ? player._htmlAudio : default;
			player.ActiveElementName = player.IsVideo ? "Video" : player.IsAudio ? "Audio" : "";

			if (player.ActiveElement != null)
			{

				player.ActiveElement.SetHtmlAttribute("src", encodedSource);
				player.ActiveElement.SetCssStyle("visibility", "visible");

				if (player.Log().IsEnabled(LogLevel.Debug))
				{
					player.Log().Debug($"{player.ActiveElementName} source changed: [{player.Source}]");
				}
				player.OnSourceLoaded?.Invoke(player, EventArgs.Empty);
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
			if (player.ActiveElement != null)
			{
				NativeMethods.SetAutoPlay(player.ActiveElement.HtmlId, (bool)args.NewValue);
			}
		}
	}

	public static DependencyProperty AreTransportControlsEnabledProperty { get; } = DependencyProperty.Register(
		"AreTransportControlsEnabled", typeof(bool), typeof(HtmlMediaPlayer), new PropertyMetadata(true,
			OnAreTransportControlsEnabledChanged));

	private static void OnAreTransportControlsEnabledChanged(DependencyObject
		dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is HtmlMediaPlayer player)
		{
			var enabled = (bool)args.NewValue;
			if (player.ActiveElement != null)
			{
				if (enabled)
				{
					if (!string.IsNullOrEmpty(player.ActiveElement.GetHtmlAttribute("controls")))
					{
						player.ActiveElement.SetHtmlAttribute("controls", "");
					}
					else
					{
						player.ActiveElement.SetHtmlAttribute("controls", "controls");
					}
				}
				else
				{
					if (!string.IsNullOrEmpty(player.ActiveElement.GetHtmlAttribute("controls")))
					{
						player.ActiveElement.SetHtmlAttribute("controls", "");
					}
				}
			}
		}
	}

	public string Source
	{
		get => (string)GetValue(SourceProperty);
		set
		{
			SetValue(SourceProperty, value);
		}
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

	internal void UpdateVideoStretch(Stretch stretch)
	{

		switch (stretch)
		{
			case Stretch.None:
				_htmlVideo.SetCssStyle("object-fit", "none");
				break;
			case Stretch.Fill:
				_htmlVideo.SetCssStyle("object-fit", "fill");
				break;
			case Stretch.Uniform:
				_htmlVideo.SetCssStyle("object-fit", "cover");
				break;
			case Stretch.UniformToFill:
				_htmlVideo.SetCssStyle("object-fit", "contain");
				break;
		}
	}

	public void Play()
	{

		TimeUpdated += OnHtmlTimeUpdated;
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Play()");
		}
		if (ActiveElement != null && !_isPlaying)
		{
			NativeMethods.Play(ActiveElement.HtmlId);
			_isPlaying = true;
		}
	}

	public void Pause()
	{
		TimeUpdated -= OnHtmlTimeUpdated;
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Pause()");
		}
		if (ActiveElement != null && _isPlaying)
		{
			NativeMethods.Pause(ActiveElement.HtmlId);
			_isPlaying = false;
		}
	}

	public void Stop()
	{
		TimeUpdated -= OnHtmlTimeUpdated;
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Stop()");
		}
		if (ActiveElement != null)
		{
			NativeMethods.Stop(ActiveElement.HtmlId);
			_isPlaying = false;
		}
	}

	private double _playbackRate;
	public double PlaybackRate
	{
		get => _playbackRate;
		set
		{
			_playbackRate = value;
			NativeMethods.SetPlaybackRate(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId, value);
		}
	}

	private bool _isLoopingEnabled;
	public void SetIsLoopingEnabled(bool value)
	{
		_isLoopingEnabled = value;
		if (ActiveElement != null)
		{
			if (_isLoopingEnabled)
			{
				ActiveElement.SetHtmlAttribute("loop", "loop");
			}
			else
			{
				ActiveElement.ClearHtmlAttribute("loop");
			}
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"{ActiveElementName} loop {_isLoopingEnabled}: [{Source}]");
			}
		}
	}
}
