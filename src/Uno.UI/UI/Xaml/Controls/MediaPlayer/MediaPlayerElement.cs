#if __IOS__ || __ANDROID__
using System;
using System.ComponentModel;
using Uno.Extensions;
using Windows.Media.Playback;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerElement : IDisposable
	{
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
				if (mpe.MediaPlayer != null)
				{
					Console.WriteLine("MEDIAPLAYERIMPL - OnSourceChanged");
					mpe.MediaPlayer.Source = args.NewValue as IMediaPlaybackSource;
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
				new FrameworkPropertyMetadata(default(ImageSource)));

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
					Console.WriteLine("MEDIAPLAYERIMPL - OnAutoPlayChanged");
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
				Console.WriteLine("MEDIAPLAYERIMPL - OnMediaPlayerChanged");
				mpe.MediaPlayer.AutoPlay = mpe.AutoPlay;
				mpe.MediaPlayer.Source = mpe.Source;
			});
		}

		#endregion

		public MediaPlayerElement() : base()
		{
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (MediaPlayer == null)
			{
				Console.WriteLine("MEDIAPLAYERIMPL - Create MediaPlayer");
				MediaPlayer = new Windows.Media.Playback.MediaPlayer();
			}

			if (AutoPlay && MediaPlayer.CurrentState == MediaPlayerState.Closed)
			{
				Console.WriteLine("MEDIAPLAYERIMPL - Let's play!");
				MediaPlayer.Play();
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
