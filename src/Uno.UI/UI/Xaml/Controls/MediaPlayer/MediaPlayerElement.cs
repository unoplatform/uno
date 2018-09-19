#if __IOS__ || __ANDROID__
using System;
using System.ComponentModel;
using Uno.Extensions;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	[TemplatePart(Name = "PosterImage", Type = typeof(Image))]
	[TemplatePart(Name = "TransportControlsPresenter", Type = typeof(ContentPresenter))]
	[TemplatePart(Name = "MediaPlayerPresenter", Type = typeof(MediaPlayerPresenter))]
	public partial class MediaPlayerElement : IDisposable
	{
		private const string PosterImageName = "PosterImage";
		private const string TransportControlsPresenterName = "TransportControlsPresenter";
		private const string MediaPlayerPresenterName = "MediaPlayerPresenter";
		
		private Image _posterImage;
		private ContentPresenter _transportControlsPresenter;
		private MediaPlayerPresenter _mediaPlayerPresenter;

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
			sender.Maybe<MediaPlayerElement>(mpe =>
			{
				var source = args.NewValue as IMediaPlaybackSource;

				if (mpe.MediaPlayer != null)
				{
					mpe.MediaPlayer.Source = source;
				}

				if (source == null && mpe._posterImage != null && mpe.PosterSource != null)
				{
					mpe._posterImage.Visibility = Visibility.Visible;
					mpe._mediaPlayerPresenter.Opacity = 0;
				}
			});
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
				new FrameworkPropertyMetadata(default(ImageSource), OnPosterSourceChanged));

		private static void OnPosterSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			sender.Maybe<MediaPlayerElement>(mpe =>
			{
				var source = args.NewValue as ImageSource;

				if (source != null && (mpe.MediaPlayer == null || mpe.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.None) && mpe._posterImage != null)
				{
					mpe._posterImage.Visibility = Visibility.Visible;
					mpe._mediaPlayerPresenter.Opacity = 0;
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

		#region MediaPlayer Property

		public Windows.Media.Playback.MediaPlayer MediaPlayer
		{
			get { return (Windows.Media.Playback.MediaPlayer)GetValue(MediaPlayerProperty); }
			set { SetValue(MediaPlayerProperty, value); }
		}

		public static DependencyProperty MediaPlayerProperty { get; } =
			DependencyProperty.Register(
				nameof(MediaPlayer),
				typeof(Windows.Media.Playback.MediaPlayer),
				typeof(MediaPlayerElement),
				new FrameworkPropertyMetadata(default(Windows.Media.Playback.MediaPlayer), OnMediaPlayerChanged));

		private static void OnMediaPlayerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			sender.Maybe<MediaPlayerElement>(mpe =>
			{
				if (args.OldValue is Windows.Media.Playback.MediaPlayer oldMediaPlayer)
				{
					oldMediaPlayer.MediaFailed -= mpe.OnMediaFailed;
					oldMediaPlayer.MediaFailed -= mpe.OnMediaOpened;
				}

				if (args.NewValue is Windows.Media.Playback.MediaPlayer newMediaPlayer)
				{
					//newMediaPlayer.AutoPlay = mpe.AutoPlay;
					newMediaPlayer.Source = mpe.Source;
					newMediaPlayer.MediaFailed += mpe.OnMediaFailed;
					newMediaPlayer.MediaOpened += mpe.OnMediaOpened;
					mpe.TransportControls?.SetMediaPlayer(newMediaPlayer);
					mpe._isTransportControlsBound = true;
				}
			});
		}

		private void OnMediaFailed(Windows.Media.Playback.MediaPlayer session, object args)
		{
			Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				if (PosterSource != null && _posterImage != null)
				{
					_posterImage.Visibility = Visibility.Visible;
					_mediaPlayerPresenter.Opacity = 0;
				}
			});
		}

		private void OnMediaOpened(Windows.Media.Playback.MediaPlayer session, object args)
		{
			Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				if (_posterImage != null)
				{
					_posterImage.Visibility = Visibility.Collapsed;
					_mediaPlayerPresenter.Opacity = 1;
				}
			});
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
				new FrameworkPropertyMetadata(false));

		#endregion

		public MediaTransportControls TransportControls { get; set; } = new MediaTransportControls();

		public MediaPlayerElement() : base()
		{
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			MediaPlayer.AutoPlay = AutoPlay;

			if(MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Opening && AutoPlay)
			{
				MediaPlayer.Play();
			}
		}
		
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_posterImage = this.GetTemplateChild(PosterImageName) as Image;
			_mediaPlayerPresenter = this.GetTemplateChild(MediaPlayerPresenterName) as MediaPlayerPresenter;
			_transportControlsPresenter = this.GetTemplateChild(TransportControlsPresenterName) as ContentPresenter;
			_transportControlsPresenter.Content = TransportControls;
			TransportControls.ApplyTemplate();

			if (MediaPlayer == null)
			{
				MediaPlayer = new Windows.Media.Playback.MediaPlayer();
			}

			if (PosterSource != null && MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.None)
			{
				_posterImage.Visibility = Visibility.Visible;
				_mediaPlayerPresenter.Opacity = 0;
			}

			if (!_isTransportControlsBound)
			{
				TransportControls?.SetMediaPlayer(MediaPlayer);
				_isTransportControlsBound = true;
			}
		}

		public void SetMediaPlayer(Windows.Media.Playback.MediaPlayer mediaPlayer)
		{
			MediaPlayer?.Dispose();
			MediaPlayer = mediaPlayer;
		}
	}
}
#endif
