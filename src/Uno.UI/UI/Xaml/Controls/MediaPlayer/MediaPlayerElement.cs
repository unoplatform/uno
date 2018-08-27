#if __IOS__ || __ANDROID__

using System;
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
				new FrameworkPropertyMetadata(default(IMediaPlaybackSource)));

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
				new FrameworkPropertyMetadata(true));

		#endregion

		#region MediaPlayer Property

		public MediaPlayer MediaPlayer
		{
			get { return (MediaPlayer)GetValue(MediaPlayerProperty); }
			set { SetValue(MediaPlayerProperty, value); }
		}

		public static DependencyProperty MediaPlayerProperty { get; } =
			DependencyProperty.Register(
				nameof(MediaPlayer),
				typeof(MediaPlayer),
				typeof(MediaPlayerElement),
				new FrameworkPropertyMetadata(default(MediaPlayer)));

		#endregion

		public MediaPlayerElement() : base()
		{
		}
	
		public void SetMediaPlayer(MediaPlayer mediaPlayer)
		{
			MediaPlayer?.Dispose();
			MediaPlayer = mediaPlayer;
		}
	}
}
#endif
