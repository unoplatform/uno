using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ExpanderTemplateSettings : DependencyObject
	{
		public double ContentHeight
		{
			get => (double)GetValue(ContentHeightProperty);
			internal set => SetValue(ContentHeightProperty, value);
		}

		public static DependencyProperty ContentHeightProperty { get; } =
			DependencyProperty.Register(
				nameof(ContentHeight),
				typeof(double),
				typeof(ExpanderTemplateSettings),
				new FrameworkPropertyMetadata(0.0));

		public double NegativeContentHeight
		{
			get => (double)GetValue(NegativeContentHeightProperty);
			internal set => SetValue(NegativeContentHeightProperty, value);
		}

		public static DependencyProperty NegativeContentHeightProperty { get; } =
			DependencyProperty.Register(
				nameof(NegativeContentHeight),
				typeof(double),
				typeof(ExpanderTemplateSettings),
				new FrameworkPropertyMetadata(0.0));
	}
}
