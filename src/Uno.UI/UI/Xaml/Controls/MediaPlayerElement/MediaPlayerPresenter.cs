using System;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerPresenter : Border
	{
		private WeakReference<MediaPlayerElement> wrOwner;

		internal void SetOwner(MediaPlayerElement owner)
		{
			wrOwner = new WeakReference<MediaPlayerElement>(owner);
		}

		private float GetScaledOtherDimension(
			float scaledOneDimension,
			uint naturalOneDimension,
			uint naturalOtherDimension)
		{
			//
			// naturalOneDimension is mapped to scaledOneDimension. Map naturalOtherDimension to scaledOtherDimension using the
			// same scale factor.
			//
			// scaledOther / naturalOther = scaledOne / naturalOne
			//                scaledOther = naturalOther * scaledOne / naturalOne
			//

			if (naturalOneDimension == 0)
			{
				return 0.0f;
			}
			else
			{
#if __IOS__
				// There are situations where between measurements scaledDimension and naturalDimension
				// have a small difference in value (a few pixels) causing the measurement to go into an infinite loop.
				// Related to: https://github.com/unoplatform/uno/issues/15254
				var ratio = (float)Math.Round(scaledOneDimension / naturalOneDimension, 1);
#else
				var ratio = scaledOneDimension / naturalOneDimension;
#endif
				return naturalOtherDimension * ratio;
			}
		}


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
				typeof(MediaPlayerPresenter),
				new FrameworkPropertyMetadata(default(global::Windows.Media.Playback.MediaPlayer), OnMediaPlayerChanged));

		private static void OnMediaPlayerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is MediaPlayerPresenter presenter)
			{
				if (presenter.Log().IsEnabled(LogLevel.Debug))
				{
					presenter.Log().LogDebug($"MediaPlayerPresenter.OnMediaPlayerChanged({args.NewValue})");
				}
				if (args.OldValue is global::Windows.Media.Playback.MediaPlayer oldPlayer)
				{
					oldPlayer.NaturalVideoDimensionChanged -= presenter.OnNaturalVideoDimensionChanged;
					oldPlayer.MediaFailed -= presenter.OnMediaFailed;
					oldPlayer.SourceChanged -= presenter.OnSourceChanged;
				}

				if (args.NewValue is global::Windows.Media.Playback.MediaPlayer newPlayer)
				{
					newPlayer.NaturalVideoDimensionChanged += presenter.OnNaturalVideoDimensionChanged;
					newPlayer.MediaFailed += presenter.OnMediaFailed;
					newPlayer.SourceChanged += presenter.OnSourceChanged;

#if __IOS__ || __ANDROID__ || __MACOS__
					presenter.SetVideoSurface(newPlayer.RenderSurface);
#endif

					presenter.OnMediaPlayerChangedPartial(newPlayer);
				}
			}
		}

		#endregion

		partial void OnMediaPlayerChangedPartial(global::Windows.Media.Playback.MediaPlayer mediaPlayer);

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
				new FrameworkPropertyMetadata(Stretch.Uniform, (s, e) => ((MediaPlayerPresenter)s).OnStretchChanged((Stretch)e.NewValue, (Stretch)e.OldValue)));

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
			InitializePartial();
		}

		partial void InitializePartial();

		/// <summary>
		/// Indicates whether or not the player is currently toggling the fullscreen mode.
		/// </summary>
		internal bool IsTogglingFullscreen { get; set; }

		private protected override void OnUnloaded()
		{
			// The control will get unloaded when going to full screen mode.
			// Similar to UWP, the video should keep playing while changing mode.
			if (!IsTogglingFullscreen)
			{
				MediaPlayer.Stop();
			}

			base.OnUnloaded();
		}

		private void OnNaturalVideoDimensionChanged(global::Windows.Media.Playback.MediaPlayer sender, object args)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				Visibility = Visibility.Visible;
			});

			InvalidateMeasure();
		}

		private void OnMediaFailed(global::Windows.Media.Playback.MediaPlayer sender, MediaPlayerFailedEventArgs args)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				Visibility = Visibility.Collapsed;
			});
		}
		private void OnSourceChanged(global::Windows.Media.Playback.MediaPlayer sender, object args)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				Visibility = Visibility.Visible;
			});
		}

		private FrameworkElement GetLayoutOwner()
		{
			if (wrOwner?.TryGetTarget(out var owner) == true && owner is not null && !IsFullWindow)
			{
				return owner;
			}

			return this;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var layoutOwner = GetLayoutOwner();
			var explicitWidth = layoutOwner.Width;
			var explicitHeight = layoutOwner.Height;
			if (!double.IsNaN(explicitWidth) && !double.IsNaN(explicitHeight))
			{
				return new Size(explicitWidth, explicitHeight);
			}

			//
			// When determining the layout size:
			//
			//   1. If the explicit size is provided, then use that.
			//
			//   2. If the explicit size is only provided in one dimension, then use the explicit size in that dimension
			//      and infer the other from the natural size of the media.
			//
			//      Note that if the media is not ready yet, then we use 0x0.
			//
			//   3. If neither dimension is provided and the stretch is None, then use the natural size of the media.
			//
			//   4. If neither dimension is provided, the stretch isn't None, and the available size is finite, then use
			//      the available size.
			//
			//   5. If neither dimension is provided, the stretch isn't None, and the available size is infinite in one
			//      dimension, then fill the available area in one dimension and infer the other from the natural size
			//      of the media.
			//
			//   6. If neither dimension is provided, the stretch isn't None, and the available size is infinite in both
			//      dimensions, use the natural size of the media.
			//
			if (!double.IsNaN(explicitWidth))
			{
				return new Size(
					explicitWidth,
					GetScaledOtherDimension((float)explicitWidth, NaturalVideoWidth, NaturalVideoHeight));
			}
			else if (!double.IsNaN(explicitHeight))
			{
				return new Size(
					GetScaledOtherDimension((float)explicitHeight, NaturalVideoHeight, NaturalVideoWidth),
					explicitHeight);
			}
			else if (Stretch == Stretch.None)
			{
				return new Size(NaturalVideoWidth, NaturalVideoHeight);
			}
			else
			{
				bool isFiniteWidth = !double.IsInfinity(availableSize.Width);
				bool isFiniteHeight = !double.IsInfinity(availableSize.Height);

				if (isFiniteWidth && isFiniteHeight)
				{
					return availableSize;
				}
				else if (isFiniteWidth)
				{
					return new Size(
						availableSize.Width,
						GetScaledOtherDimension((float)availableSize.Width, NaturalVideoWidth, NaturalVideoHeight));
				}
				else if (isFiniteHeight)
				{
					return new Size(
						GetScaledOtherDimension((float)availableSize.Height, NaturalVideoHeight, NaturalVideoWidth),
						availableSize.Height);
				}
				else
				{
					return new Size(NaturalVideoWidth, NaturalVideoHeight);
				}
			}
		}
	}
}
