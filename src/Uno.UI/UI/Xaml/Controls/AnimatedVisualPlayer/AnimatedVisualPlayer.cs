using System;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = "Source")]
	public partial class AnimatedVisualPlayer : FrameworkElement
	{
		// True while the source has been paused because the player became hidden (Visibility=Collapsed
		// or removed from the tree). When we transition back to visible, this flag tells us we should
		// resume playback automatically — matching the behavior of the native WinUI AnimatedVisualPlayer
		// (see AnimationPlay::OnHiding / OnUnhiding in microsoft-ui-xaml).
		private bool _wasPlayingWhenHidden;
		private bool _isEffectivelyVisible;

		public static DependencyProperty AutoPlayProperty { get; } = DependencyProperty.Register(
			"AutoPlay", typeof(bool), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(true, UpdateSourceOnChanged));

		public static DependencyProperty IsAnimatedVisualLoadedProperty { get; } = DependencyProperty.Register(
			"IsAnimatedVisualLoaded", typeof(bool), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(false));

		public static DependencyProperty IsPlayingProperty { get; } = DependencyProperty.Register(
			"IsPlaying", typeof(bool), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(false, OnIsPlayingChanged));

		public static DependencyProperty PlaybackRateProperty { get; } = DependencyProperty.Register(
			"PlaybackRate", typeof(double), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(1.0, UpdateSourceOnChanged));

		public static DependencyProperty FallbackContentProperty { get; } = DependencyProperty.Register(
			"FallbackContent", typeof(DataTemplate), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, UpdateSourceOnChanged));

		public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
			"Source", typeof(IAnimatedVisualSource), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, UpdateSourceOnChanged));

		public static DependencyProperty StretchProperty { get; } = DependencyProperty.Register(
			"Stretch", typeof(Stretch), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, UpdateSourceOnChanged));

		public static DependencyProperty DurationProperty { get; } = DependencyProperty.Register(
			"Duration", typeof(TimeSpan), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(default(TimeSpan)));

		public bool AutoPlay
		{
			get => (bool)GetValue(AutoPlayProperty);
			set => SetValue(AutoPlayProperty, value);
		}

		public bool IsAnimatedVisualLoaded
		{
			get => (bool)GetValue(IsAnimatedVisualLoadedProperty);
			internal set => SetValue(IsAnimatedVisualLoadedProperty, value);
		}

		public bool IsPlaying
		{
			get => (bool)GetValue(IsPlayingProperty);
			internal set => SetValue(IsPlayingProperty, value);
		}

		public DataTemplate FallbackContent
		{
			get => (DataTemplate)GetValue(FallbackContentProperty);
			set => SetValue(FallbackContentProperty, value);
		}

		public double PlaybackRate
		{
			get => (double)GetValue(PlaybackRateProperty);
			set => SetValue(PlaybackRateProperty, value);
		}

		public IAnimatedVisualSource Source
		{
			get => (IAnimatedVisualSource)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public Stretch Stretch
		{
			get => (Stretch)GetValue(StretchProperty);
			set => SetValue(StretchProperty, value);
		}

		public TimeSpan Duration
		{
			get => (TimeSpan)GetValue(DurationProperty);
			internal set => SetValue(DurationProperty, value);
		}

		private static void UpdateSourceOnChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		{
			if (source is AnimatedVisualPlayer { IsLoaded: true } player)
			{
				if (player.Source != null)
				{
					player.Source.Update(player);
					player.InvalidateMeasure();
				}
			}
		}

		private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			// IsPlaying is set internally by the source. When it transitions to true while the player
			// is not effectively visible (e.g. the source's AutoPlay fires after the player was loaded
			// while collapsed), pause immediately so the rendering loop does not stay active for a
			// surface no one can see.
			if (d is AnimatedVisualPlayer player
				&& args.NewValue is true
				&& !player._isEffectivelyVisible
				&& player.IsLoaded)
			{
				player._wasPlayingWhenHidden = true;
				player.Source?.Pause();
			}
		}

		public void Pause()
		{
			// Explicit user action — do not auto-resume on next visibility transition.
			_wasPlayingWhenHidden = false;
			Source?.Pause();
		}

		public IAsyncAction PlayAsync(double fromProgress, double toProgress, bool looped)
		{
			_wasPlayingWhenHidden = false;
			Source?.Play(fromProgress, toProgress, looped);
			return Task.CompletedTask.AsAsyncAction();
		}

		public void Resume()
		{
			_wasPlayingWhenHidden = false;
			Source?.Resume();
		}

		public void SetProgress(double progress) => Source?.SetProgress(progress);

		public void Stop()
		{
			_wasPlayingWhenHidden = false;
			Source?.Stop();
		}

		private protected override void OnLoaded()
		{
			// Compute visibility before notifying the source, since Source.Update may synchronously
			// trigger Play (e.g. for embedded JSON), which fires OnIsPlayingChanged and consults
			// _isEffectivelyVisible to decide whether to immediately pause.
			_isEffectivelyVisible = ComputeIsEffectivelyVisible();

			Source?.Update(this);
			Source?.Load();

			base.OnLoaded();
		}

		private protected override void OnUnloaded()
		{
			Source?.Unload();

			base.OnUnloaded();
		}

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);
			UpdateEffectiveVisibility();
		}

		private bool ComputeIsEffectivelyVisible() => IsLoaded && Visibility == Visibility.Visible;

		private void UpdateEffectiveVisibility()
		{
			var newValue = ComputeIsEffectivelyVisible();
			if (newValue == _isEffectivelyVisible)
			{
				return;
			}

			_isEffectivelyVisible = newValue;

			if (newValue)
			{
				// Becoming visible — resume playback if we paused it ourselves when going hidden.
				if (_wasPlayingWhenHidden)
				{
					_wasPlayingWhenHidden = false;
					Source?.Resume();
				}
			}
			else
			{
				// Becoming hidden — pause if currently playing so the rendering loop can idle.
				// Mirrors AnimationPlay::OnHiding in the WinUI native player.
				if (IsPlaying)
				{
					_wasPlayingWhenHidden = true;
					Source?.Pause();
				}
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (Source?.Measure(availableSize) != null)
			{
				return Source.Measure(availableSize);
			}
			else
			{
				return MeasureFirstChild(availableSize);
			}
		}

		protected override Size ArrangeOverride(Size finalSize) => ArrangeFirstChild(finalSize);
	}
}
