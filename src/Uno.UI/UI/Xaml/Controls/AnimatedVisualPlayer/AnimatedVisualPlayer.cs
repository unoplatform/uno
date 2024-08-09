using System;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	[ContentProperty(Name = "Source")]
	public partial class AnimatedVisualPlayer : FrameworkElement
	{
		public static DependencyProperty AutoPlayProperty { get; } = DependencyProperty.Register(
			"AutoPlay", typeof(bool), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(true, UpdateSourceOnChanged));

		public static DependencyProperty IsAnimatedVisualLoadedProperty { get; } = DependencyProperty.Register(
			"IsAnimatedVisualLoaded", typeof(bool), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(false, UpdateSourceOnChanged));

		public static DependencyProperty IsPlayingProperty { get; } = DependencyProperty.Register(
			"IsPlaying", typeof(bool), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(false, UpdateSourceOnChanged));

		public static DependencyProperty PlaybackRateProperty { get; } = DependencyProperty.Register(
			"PlaybackRate", typeof(double), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(1.0, UpdateSourceOnChanged));

		public static DependencyProperty FallbackContentProperty { get; } = DependencyProperty.Register(
			"FallbackContent", typeof(DataTemplate), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, UpdateSourceOnChanged));

		public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
			"Source", typeof(IAnimatedVisualSource), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, UpdateSourceOnChanged));

		public static DependencyProperty StretchProperty { get; } = DependencyProperty.Register(
			"Stretch", typeof(Stretch), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, UpdateSourceOnChanged));

		public static DependencyProperty DurationProperty { get; } = DependencyProperty.Register(
			"Duration", typeof(TimeSpan), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(default(TimeSpan), UpdateSourceOnChanged));

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

		public void Pause() => Source?.Pause();

		public IAsyncAction PlayAsync(double fromProgress, double toProgress, bool looped)
		{
			Source?.Play(fromProgress, toProgress, looped);
			return Task.CompletedTask.AsAsyncAction();
		}

		public void Resume() => Source?.Resume();

		public void SetProgress(double progress) => Source?.SetProgress(progress);

		public void Stop() => Source?.Stop();

		private protected override void OnLoaded()
		{
			Source?.Update(this);
			Source?.Load();

			base.OnLoaded();
		}

		private protected override void OnUnloaded()
		{
			Source?.Unload();

			base.OnUnloaded();
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
