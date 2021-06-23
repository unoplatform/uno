using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public class AnimatedIconSource : IconSource
	{
		public IconSource FallbackIconSource
		{
			get => (IconSource)GetValue(FallbackIconSourceProperty);
			set => SetValue(FallbackIconSourceProperty, value);
		}

		public static DependencyProperty FallbackIconSourceProperty { get; } =
			DependencyProperty.Register(nameof(FallbackIconSource), typeof(IconSource), typeof(AnimatedIconSource), new PropertyMetadata(null, OnFallbackIconSourcePropertyChanged));

		public bool MirroredWhenRightToLeft
		{
			get => (bool)GetValue(MirroredWhenRightToLeftProperty);
			set => SetValue(MirroredWhenRightToLeftProperty, value);
		}

		public static DependencyProperty MirroredWhenRightToLeftProperty { get; } =
			DependencyProperty.Register(nameof(MirroredWhenRightToLeft), typeof(bool), typeof(AnimatedIconSource), new PropertyMetadata(false, OnMirroredWhenRightToLeftPropertyChanged));

		public IAnimatedVisualSource2 Source
		{
			get => (IAnimatedVisualSource2)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public static DependencyProperty SourceProperty { get; } =
			DependencyProperty.Register(nameof(Source), typeof(IAnimatedVisualSource2), typeof(AnimatedIconSource), new PropertyMetadata(null, OnSourcePropertyChanged));

		public override IconElement CreateIconElement()
		{
			AnimatedIcon animatedIcon = new AnimatedIcon();
			if (Source is { } source)
			{
				animatedIcon.Source = source;
			}
			if (FallbackIconSource is { } fallbackIconSource)
			{
				animatedIcon.FallbackIconSource = fallbackIconSource;
			}
			if (Foreground is { } newForeground)
			{
				animatedIcon.Foreground = newForeground;
			}
			animatedIcon.MirroredWhenRightToLeft = MirroredWhenRightToLeft;

			return animatedIcon;
		}

		protected override DependencyProperty GetIconElementProperty(DependencyProperty sourceProperty)
		{
			if (sourceProperty == SourceProperty)
			{
				return AnimatedIcon.SourceProperty;
			}
			else if (sourceProperty == FallbackIconSourceProperty)
			{
				return AnimatedIcon.FallbackIconSourceProperty;
			}
			else if (sourceProperty == MirroredWhenRightToLeftProperty)
			{
				return AnimatedIcon.MirroredWhenRightToLeftProperty;
			}

			return base.GetIconElementProperty(sourceProperty);
		}

	}
}
