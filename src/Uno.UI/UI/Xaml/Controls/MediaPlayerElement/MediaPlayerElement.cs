﻿using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls
{
	[TemplatePart(Name = PosterImageName, Type = typeof(Image))]
	[TemplatePart(Name = TransportControlsPresenterName, Type = typeof(ContentPresenter))]
	[TemplatePart(Name = MediaPlayerPresenterName, Type = typeof(MediaPlayerPresenter))]
	[TemplatePart(Name = LayoutRootName, Type = typeof(Grid))]
	public partial class MediaPlayerElement
	{
		private const string PosterImageName = "PosterImage";
		private const string TransportControlsPresenterName = "TransportControlsPresenter";
		private const string MediaPlayerPresenterName = "MediaPlayerPresenter";
		private const string LayoutRootName = "LayoutRoot";

		private Image _posterImage;
		private ContentPresenter _transportControlsPresenter;
		private MediaPlayerPresenter _mediaPlayerPresenter;
		private Grid _layoutRoot;
		private CompositeDisposable _mediaPlayerDisposable;

		private bool _isTransportControlsBound;

		#region Source Property

		public IMediaPlaybackSource Source
		{
			get { return (IMediaPlaybackSource)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		public static DependencyProperty SourceProperty { get; } =
			DependencyProperty.Register(
				nameof(Source),
				typeof(IMediaPlaybackSource),
				typeof(MediaPlayerElement),
				new FrameworkPropertyMetadata(default(IMediaPlaybackSource), OnSourceChanged));

		private static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is MediaPlayerElement mpe)
			{
				if (mpe.Log().IsEnabled(LogLevel.Debug))
				{
					mpe.Log().LogDebug($"MediaPlayerElement.SourceChanged({args.NewValue})");
				}
				var source = args.NewValue as IMediaPlaybackSource;

				if (mpe.MediaPlayer != null)
				{
					mpe.MediaPlayer.Source = source;
				}

				if (source == null)
				{
					mpe.ShowPosterImage(true);
				}
			}
		}

		#endregion

		#region PosterSource Property

		public ImageSource PosterSource
		{
			get { return (ImageSource)GetValue(PosterSourceProperty); }
			set { SetValue(PosterSourceProperty, value); }
		}

		public static DependencyProperty PosterSourceProperty { get; } =
			DependencyProperty.Register(
				nameof(PosterSource),
				typeof(ImageSource),
				typeof(MediaPlayerElement),
				new FrameworkPropertyMetadata(default(ImageSource), FrameworkPropertyMetadataOptions.AffectsMeasure, OnPosterSourceChanged));

		private static void OnPosterSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			sender.Maybe<MediaPlayerElement>(mpe =>
			{
				if (mpe.MediaPlayer == null || mpe.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.None)
				{
					mpe.ShowPosterImage(true);
				}
			});
		}

		#endregion

		#region AutoPlay Property

		public bool AutoPlay
		{
			get { return (bool)GetValue(AutoPlayProperty); }
			set { SetValue(AutoPlayProperty, value); }
		}

		public static DependencyProperty AutoPlayProperty { get; } =
			DependencyProperty.Register(
				nameof(AutoPlay),
				typeof(bool),
				typeof(MediaPlayerElement),
				new FrameworkPropertyMetadata(true, OnAutoPlayChanged));

		private static void OnAutoPlayChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			sender.Maybe<MediaPlayerElement>(mpe =>
			{
				if (mpe.MediaPlayer != null)
				{
					mpe.MediaPlayer.AutoPlay = (bool)args.NewValue;
				}
			});
		}

		#endregion

		#region IsFullWindow Property

		public bool IsFullWindow
		{
			get { return (bool)GetValue(IsFullWindowProperty); }
			set { SetValue(IsFullWindowProperty, value); }
		}

		public static DependencyProperty IsFullWindowProperty { get; } =
			DependencyProperty.Register(
				nameof(IsFullWindow),
				typeof(bool),
				typeof(MediaPlayerElement),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure, OnIsFullWindowChanged));


		private static void OnIsFullWindowChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			sender.Maybe<MediaPlayerElement>(mpe =>
			{
				mpe.ToggleFullScreen((bool)args.NewValue);
			});
		}

		private void ToggleFullScreen(bool showFullscreen)
		{
			try
			{
				if (XamlRoot?.HostWindow is null)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning(
							$"Cannot toggle Full Screen as the media player was not yet " +
							$"loaded in the visual tree.");
					}

					return;
				}

				_mediaPlayerPresenter.IsTogglingFullscreen = true;

				if (showFullscreen)
				{
					ApplicationView.GetForWindowId(XamlRoot.HostWindow.AppWindow.Id).TryEnterFullScreenMode();

#if __ANDROID__
					this.RemoveView(_layoutRoot);
#elif __APPLE_UIKIT__
					_layoutRoot.RemoveFromSuperview();
#else
					_mediaPlayerPresenter?.RequestFullScreen();
#endif
#if !__NETSTD_REFERENCE__ && !IS_UNIT_TESTS
					XamlRoot.VisualTree.FullWindowMediaRoot.DisplayFullscreen(_layoutRoot);
#endif
				}
				else
				{
					ApplicationView.GetForWindowId(XamlRoot.HostWindow.AppWindow.Id).ExitFullScreenMode();
#if !__NETSTD_REFERENCE__ && !IS_UNIT_TESTS
					XamlRoot.VisualTree.FullWindowMediaRoot.DisplayFullscreen(null);
#endif

#if __ANDROID__
					this.AddView(_layoutRoot);
#elif __APPLE_UIKIT__
					this.Add(_layoutRoot);
#elif __SKIA__ || __WASM__
					this.AddChild(_layoutRoot);
					_mediaPlayerPresenter?.ExitFullScreen();
#else
					_mediaPlayerPresenter?.ExitFullScreen();
#endif
				}
				TransportControls.SetMeasureCommandBar();

			}
			finally
			{
				_mediaPlayerPresenter.IsTogglingFullscreen = false;
			}
		}

		#endregion

		#region MediaPlayer Property

		public global::Windows.Media.Playback.MediaPlayer MediaPlayer
		{
			get { return (global::Windows.Media.Playback.MediaPlayer)GetValue(MediaPlayerProperty); }
			set { SetValue(MediaPlayerProperty, value); }
		}

		public static DependencyProperty MediaPlayerProperty { get; } =
			DependencyProperty.Register(
				nameof(MediaPlayer),
				typeof(global::Windows.Media.Playback.MediaPlayer),
				typeof(MediaPlayerElement),
				new FrameworkPropertyMetadata(default(global::Windows.Media.Playback.MediaPlayer), OnMediaPlayerChanged));

		private static void OnMediaPlayerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is MediaPlayerElement mpe)
			{
				mpe._mediaPlayerDisposable?.Dispose();
				mpe._mediaPlayerDisposable = null;

				if (args.NewValue is global::Windows.Media.Playback.MediaPlayer newMediaPlayer)
				{
					newMediaPlayer.Source = mpe.Source;

					mpe._mediaPlayerDisposable = new CompositeDisposable();
					var weakThis = new WeakReference<MediaPlayerElement>(mpe);
#pragma warning disable IDE0055
					TypedEventHandler<global::Windows.Media.Playback.MediaPlayer,MediaPlayerFailedEventArgs> newMediaPlayerOnMediaFailed = (s, e) => { if (weakThis.TryGetTarget(out var mpe)) { mpe.OnMediaFailed(s, e); } };
					newMediaPlayer.MediaFailed += newMediaPlayerOnMediaFailed;
					mpe._mediaPlayerDisposable.Add(Disposable.Create(() => newMediaPlayer.MediaFailed -= newMediaPlayerOnMediaFailed));
					TypedEventHandler<global::Windows.Media.Playback.MediaPlayer,object> newMediaPlayerOnMediaOpened = (s, e) => { if (weakThis.TryGetTarget(out var mpe)) { mpe.OnMediaOpened(s, e); } };
					newMediaPlayer.MediaOpened += newMediaPlayerOnMediaOpened;
					mpe._mediaPlayerDisposable.Add(Disposable.Create(() => newMediaPlayer.MediaOpened -= newMediaPlayerOnMediaOpened));
					TypedEventHandler<global::Windows.Media.Playback.MediaPlayer,object> newMediaPlayerOnNaturalVideoDimensionChanged = (s, e) => { if (weakThis.TryGetTarget(out var mpe)) { mpe.OnNaturalVideoDimensionChanged(s, e); } };
					newMediaPlayer.NaturalVideoDimensionChanged += newMediaPlayerOnNaturalVideoDimensionChanged;
					mpe._mediaPlayerDisposable.Add(Disposable.Create(() => newMediaPlayer.NaturalVideoDimensionChanged -= newMediaPlayerOnNaturalVideoDimensionChanged));
#pragma warning restore IDE0055

					mpe.TransportControls?.SetMediaPlayer(newMediaPlayer);
					mpe._isTransportControlsBound = true;
				}
			}
		}

		private void OnNaturalVideoDimensionChanged(global::Windows.Media.Playback.MediaPlayer sender, object args)
		{
			_ = Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() => ShowPosterImage(!sender.IsVideo));
		}

		private void OnMediaFailed(global::Windows.Media.Playback.MediaPlayer sender, object args)
		{
			_ = Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() => ShowPosterImage(true));
		}

		private void OnMediaOpened(global::Windows.Media.Playback.MediaPlayer sender, object args)
		{
			_ = Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() => ShowPosterImage(!sender.IsVideo));
		}

		#endregion

		#region AreTransportControlsEnabled Property

		public bool AreTransportControlsEnabled
		{
			get { return (bool)GetValue(AreTransportControlsEnabledProperty); }
			set { SetValue(AreTransportControlsEnabledProperty, value); }
		}

		public static DependencyProperty AreTransportControlsEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(AreTransportControlsEnabled),
				typeof(bool),
				typeof(MediaPlayerElement),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

		#endregion

		#region Stretch Property

		public Stretch Stretch
		{
			get { return (Stretch)GetValue(StretchProperty); }
			set { SetValue(StretchProperty, value); }
		}

		public static DependencyProperty StretchProperty { get; } =
			DependencyProperty.Register(
				nameof(Stretch),
				typeof(Stretch),
				typeof(MediaPlayerElement),
				new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure));

		#endregion

		private MediaTransportControls _transportControls;
		public MediaTransportControls TransportControls
		{
			get
			{
				return _transportControls;
			}
			set
			{
				_transportControls = value;
				_transportControls.SetMediaPlayerElement(this);
			}
		}

		public MediaPlayerElement() : base()
		{
			TransportControls = new MediaTransportControls();

			DefaultStyleKey = typeof(MediaPlayerElement);

			Initialize();
		}

		partial void Initialize();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			if (MediaPlayer != null)
			{
				MediaPlayer.AutoPlay = AutoPlay;

				if (MediaPlayer.PlaybackSession.PlaybackState is not MediaPlaybackState.None
					&& AutoPlay)
				{
					MediaPlayer.Play();
				}
			}
		}

		// The PosterSource is displayed in the following situations:
		//  - When a valid source is not set.For example, Source is not set, Source was set to Null, or the source is invalid (as is the case when a MediaFailed event fires).
		//  - While media is loading. For example, a valid source is set, but the MediaOpened event has not fired yet.
		//  - When media is streaming to another device.
		//  - When the media is audio only.
		private void ShowPosterImage(bool showPoster)
		{
			if (PosterSource != null)
			{
				if (_posterImage != null)
				{
					_posterImage.Visibility = showPoster ? Visibility.Visible : Visibility.Collapsed;
				}

				if (_mediaPlayerPresenter != null)
				{
					_mediaPlayerPresenter.Opacity = showPoster ? 0 : 1;
				}
			}
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_layoutRoot = this.GetTemplateChild(LayoutRootName) as Grid;
			_posterImage = this.GetTemplateChild(PosterImageName) as Image;
			_mediaPlayerPresenter = this.GetTemplateChild(MediaPlayerPresenterName) as MediaPlayerPresenter;
			_mediaPlayerPresenter?.SetOwner(this);

			_transportControlsPresenter = this.GetTemplateChild(TransportControlsPresenterName) as ContentPresenter;
			_transportControlsPresenter.Content = TransportControls;
			TransportControls.ApplyTemplate();

			if (MediaPlayer == null)
			{
				MediaPlayer = new global::Windows.Media.Playback.MediaPlayer();
				_mediaPlayerPresenter?.ApplyStretch();
			}

			// For video content, show the poster source until it is ready to be displayed.
			ShowPosterImage(true);

			if (!_isTransportControlsBound)
			{
				TransportControls?.SetMediaPlayer(MediaPlayer);
				_isTransportControlsBound = true;
			}
		}

		public void SetMediaPlayer(global::Windows.Media.Playback.MediaPlayer mediaPlayer)
		{
			MediaPlayer = mediaPlayer;
		}

		public void ToggleCompactOverlay(bool showCompactOverlay)
		{
#if __WASM__
			if (_mediaPlayerPresenter != null)
			{
				if (showCompactOverlay)
				{
					_mediaPlayerPresenter.RequestCompactOverlay();
				}
				else
				{
					_mediaPlayerPresenter.ExitCompactOverlay();
				}
			}
#endif
		}

	}
}
