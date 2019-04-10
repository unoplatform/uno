using System;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Source")]
	public partial class AnimatedVisualPlayer : FrameworkElement
	{
		public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register(
			"AutoPlay", typeof(bool), typeof(AnimatedVisualPlayer), new PropertyMetadata(true, UpdateSourceOnChanged));

		public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
			"IsPlaying", typeof(bool), typeof(AnimatedVisualPlayer), new PropertyMetadata(false, UpdateSourceOnChanged));

		public static readonly DependencyProperty PlaybackRateProperty = DependencyProperty.Register(
			"PlaybackRate", typeof(double), typeof(AnimatedVisualPlayer), new PropertyMetadata(1.0, UpdateSourceOnChanged));

		public static readonly DependencyProperty FallbackContentProperty = DependencyProperty.Register(
			"FallbackContent", typeof(DataTemplate), typeof(AnimatedVisualPlayer), new PropertyMetadata(null, UpdateSourceOnChanged));

		public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
			"Source", typeof(IAnimatedVisualSource), typeof(AnimatedVisualPlayer), new PropertyMetadata(null, UpdateSourceOnChanged));

		public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
			"Stretch", typeof(Stretch), typeof(AnimatedVisualPlayer), new PropertyMetadata(Stretch.Uniform, UpdateSourceOnChanged));

		public bool AutoPlay
		{
			get => (bool)GetValue(AutoPlayProperty);
			set => SetValue(AutoPlayProperty, value);
		}

		public bool IsPlaying
		{
			get => (bool)GetValue(AutoPlayProperty);
			set => SetValue(AutoPlayProperty, value);
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

		private static void UpdateSourceOnChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		{
			var player = (source as AnimatedVisualPlayer);

			if (player.IsLoaded)
			{
				player?.Source?.Update(player);
			}
		}

		public void Pause()
		{
			Source?.Pause();
		}

		public async Task PlayAsync(double fromProgress, double toProgress, bool looped)
		{
			Source?.Play(looped);
		}

		public void Resume()
		{
			Source?.Resume();
		}

		public void SetProgress(double progress)
		{
			Source?.SetProgress(progress);
		}

		public void Stop()
		{
			Source?.Stop();
		}

		protected override void OnLoaded()
		{
			Source?.Update(this);
			Source?.Load();

			base.OnLoaded();
		}

		protected override void OnUnloaded()
		{
			Source?.Unload();

			base.OnUnloaded();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return Source == null
				? base.MeasureOverride(availableSize)
				: Source.Measure(availableSize);
		}}
}
