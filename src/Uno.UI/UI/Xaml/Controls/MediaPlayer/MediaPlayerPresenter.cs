#if __ANDROID__ || __IOS__
using System;
using Windows.Media.Playback;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public  partial class MediaPlayerPresenter : Border
	{
		#region MediaPlayer Property

		public MediaPlayer MediaPlayer
		{
			get { return (MediaPlayer)GetValue(MediaPlayerProperty); }
			set { SetValue(MediaPlayerProperty, value); }
		}

		public static DependencyProperty MediaPlayerProperty { get; } =
			DependencyProperty.Register(
				nameof(MediaPlayer),
				typeof(bool),
				typeof(MediaPlayerPresenter),
				new FrameworkPropertyMetadata(default(MediaPlayer), OnMediaPlayerChanged));

		private static void OnMediaPlayerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is MediaPlayerPresenter presenter &&
				args.NewValue is MediaPlayer mediaPlayer)
			{
				presenter.Child = mediaPlayer.GetSurface();
			}
		}

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
				typeof(MediaPlayerPresenter),
				new FrameworkPropertyMetadata(default(Stretch)));

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
				typeof(MediaPlayerPresenter),
				new FrameworkPropertyMetadata(false));

		#endregion

		public MediaPlayerPresenter() : base()
		{
		}
	}
}
#endif
