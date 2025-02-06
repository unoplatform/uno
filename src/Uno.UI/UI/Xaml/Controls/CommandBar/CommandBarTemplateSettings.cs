using Windows.Foundation;
using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class CommandBarTemplateSettings : DependencyObject
	{
		internal CommandBarTemplateSettings()
		{

		}

		public double ContentHeight
		{
			get => (double)GetValue(ContentHeightProperty);
			internal set => SetValue(ContentHeightProperty, value);
		}

		internal static DependencyProperty ContentHeightProperty { get; } =
			DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public Visibility EffectiveOverflowButtonVisibility
		{
			get => (Visibility)GetValue(EffectiveOverflowButtonVisibilityProperty);
			internal set => SetValue(EffectiveOverflowButtonVisibilityProperty, value);
		}

		internal static DependencyProperty EffectiveOverflowButtonVisibilityProperty { get; } =
			DependencyProperty.Register(nameof(EffectiveOverflowButtonVisibility), typeof(Visibility), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(default(Visibility)));

		public double NegativeOverflowContentHeight
		{
			get => (double)GetValue(NegativeOverflowContentHeightProperty);
			internal set => SetValue(NegativeOverflowContentHeightProperty, value);
		}

		internal static DependencyProperty NegativeOverflowContentHeightProperty { get; } =
			DependencyProperty.Register(nameof(NegativeOverflowContentHeight), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public Rect OverflowContentClipRect
		{
			get => (Rect)GetValue(OverflowContentClipRectProperty);
			internal set => SetValue(OverflowContentClipRectProperty, value);
		}

		internal static DependencyProperty OverflowContentClipRectProperty { get; } =
			DependencyProperty.Register(nameof(OverflowContentClipRect), typeof(Rect), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(new Rect()));


		public double OverflowContentHeight
		{
			get => (double)GetValue(OverflowContentHeightProperty);
			internal set => SetValue(OverflowContentHeightProperty, value);
		}

		internal static DependencyProperty OverflowContentHeightProperty { get; } =
			DependencyProperty.Register(nameof(OverflowContentHeight), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public double OverflowContentHorizontalOffset
		{
			get => (double)GetValue(OverflowContentHorizontalOffsetProperty);
			internal set => SetValue(OverflowContentHorizontalOffsetProperty, value);
		}

		internal static DependencyProperty OverflowContentHorizontalOffsetProperty { get; } =
			DependencyProperty.Register(nameof(OverflowContentHorizontalOffset), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public double OverflowContentMaxHeight
		{
			get => (double)GetValue(OverflowContentMaxHeightProperty);
			internal set => SetValue(OverflowContentMaxHeightProperty, value);
		}

		internal static DependencyProperty OverflowContentMaxHeightProperty { get; } =
			DependencyProperty.Register(nameof(OverflowContentMaxHeight), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public double OverflowContentMinWidth
		{
			get => (double)GetValue(OverflowContentMinWidthProperty);
			internal set => SetValue(OverflowContentMinWidthProperty, value);
		}

		internal static DependencyProperty OverflowContentMinWidthProperty { get; } =
			DependencyProperty.Register(nameof(OverflowContentMinWidth), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public double OverflowContentMaxWidth
		{
			get => (double)GetValue(OverflowContentMaxWidthProperty);
			internal set => SetValue(OverflowContentMaxWidthProperty, value);
		}

		internal static DependencyProperty OverflowContentMaxWidthProperty { get; } =
			DependencyProperty.Register(nameof(OverflowContentMaxWidth), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));


		public double OverflowContentCompactYTranslation
		{
			get => (double)GetValue(OverflowContentCompactYTranslationProperty);
			internal set => SetValue(OverflowContentCompactYTranslationProperty, value);
		}

		internal static DependencyProperty OverflowContentCompactYTranslationProperty { get; } =
			DependencyProperty.Register(nameof(OverflowContentCompactYTranslation), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));


		public double OverflowContentHiddenYTranslation
		{
			get => (double)GetValue(OverflowContentHiddenYTranslationProperty);
			internal set => SetValue(OverflowContentHiddenYTranslationProperty, value);
		}

		internal static DependencyProperty OverflowContentHiddenYTranslationProperty { get; } =
			DependencyProperty.Register(nameof(OverflowContentHiddenYTranslation), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public double OverflowContentMinimalYTranslation
		{
			get => (double)GetValue(OverflowContentMinimalYTranslationProperty);
			internal set => SetValue(OverflowContentMinimalYTranslationProperty, value);
		}

		internal static DependencyProperty OverflowContentMinimalYTranslationProperty { get; } =
			DependencyProperty.Register(nameof(OverflowContentMinimalYTranslation), typeof(double), typeof(CommandBarTemplateSettings), new FrameworkPropertyMetadata(0.0));
	}
}
