// MUX Reference TeachingTip.properties.cpp, commit de78834

#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a TeachingTip.
	/// </summary>
	public partial class TeachingTipTemplateSettings : DependencyObject
    {
		public IconElement IconElement
		{
			get => (IconElement)GetValue(IconElementProperty);
			set => SetValue(IconElementProperty, value);
		}

		public static DependencyProperty IconElementProperty { get; } =
			DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(TeachingTipTemplateSettings), new PropertyMetadata(null));

		public Thickness TopLeftHighlightMargin
		{
			get => (Thickness)GetValue(TopLeftHighlightMarginProperty);
			set => SetValue(TopLeftHighlightMarginProperty, value);
		}

		public static DependencyProperty TopLeftHighlightMarginProperty { get; } =
			DependencyProperty.Register(nameof(TopLeftHighlightMargin), typeof(Thickness), typeof(TeachingTipTemplateSettings), new PropertyMetadata(default(Thickness)));

		public Thickness TopRightHighlightMargin
		{
			get => (Thickness)GetValue(TopRightHighlightMarginProperty);
			set => SetValue(TopRightHighlightMarginProperty, value);
		}

		public static DependencyProperty TopRightHighlightMarginProperty { get; } =
			DependencyProperty.Register(nameof(TopRightHighlightMargin), typeof(Thickness), typeof(TeachingTipTemplateSettings), new PropertyMetadata(default(Thickness)));
	}
}
