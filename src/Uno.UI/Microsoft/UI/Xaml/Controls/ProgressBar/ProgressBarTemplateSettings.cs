using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class ProgressBarTemplateSettings : DependencyObject
	{
		public static DependencyProperty ContainerAnimationStartPositionProperty { get; } = DependencyProperty.Register(
			nameof(ContainerAnimationStartPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

		public double ContainerAnimationStartPosition
		{
			get => (double)GetValue(ContainerAnimationStartPositionProperty);
			set => SetValue(ContainerAnimationStartPositionProperty, value);
		}

		public static DependencyProperty ContainerAnimationEndPositionProperty { get; } = DependencyProperty.Register(
			nameof(ContainerAnimationEndPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

		public double ContainerAnimationEndPosition
		{
			get => (double)GetValue(ContainerAnimationEndPositionProperty);
			set => SetValue(ContainerAnimationEndPositionProperty, value);
		}

		public static DependencyProperty ContainerAnimationStartPosition2Property { get; } = DependencyProperty.Register(
			nameof(ContainerAnimationStartPosition2), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

		public double ContainerAnimationStartPosition2
		{
			get => (double)GetValue(ContainerAnimationStartPosition2Property);
			set => SetValue(ContainerAnimationStartPosition2Property, value);
		}

		public static DependencyProperty ContainerAnimationEndPosition2Property { get; } = DependencyProperty.Register(
			nameof(ContainerAnimationEndPosition2), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

		public double ContainerAnimationEndPosition2
		{
			get => (double)GetValue(ContainerAnimationEndPosition2Property);
			set => SetValue(ContainerAnimationEndPosition2Property, value);
		}

		public static DependencyProperty ContainerAnimationMidPositionProperty { get; } = DependencyProperty.Register(
			nameof(ContainerAnimationMidPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

		public double ContainerAnimationMidPosition
		{
			get => (double)GetValue(ContainerAnimationMidPositionProperty);
			set => SetValue(ContainerAnimationMidPositionProperty, value);
		}

		public static DependencyProperty IndicatorLengthDeltaProperty { get; } = DependencyProperty.Register(
			nameof(IndicatorLengthDelta), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

		public double IndicatorLengthDelta
		{
			get => (double)GetValue(IndicatorLengthDeltaProperty);
			set => SetValue(IndicatorLengthDeltaProperty, value);
		}

		public static DependencyProperty ClipRectProperty { get; } = DependencyProperty.Register(
			nameof(ClipRect), typeof(RectangleGeometry), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(RectangleGeometry)));

		public RectangleGeometry ClipRect
		{
			get => (RectangleGeometry)GetValue(ClipRectProperty);
			set => SetValue(ClipRectProperty, value);
		}
	}
}
