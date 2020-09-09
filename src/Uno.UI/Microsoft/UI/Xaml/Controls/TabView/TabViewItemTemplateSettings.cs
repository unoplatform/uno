// MUX Reference: TabViewItemTemplateSettings.properties.cpp, commit 8aaf7f8

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TabViewItemTemplateSettings : DependencyObject
    {
		public IconElement IconElement
		{
			get => (IconElement)GetValue(IconElementProperty);
			set => SetValue(IconElementProperty, value);
		}

		public static DependencyProperty IconElementProperty { get; } =
			DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(TabViewItemTemplateSettings), new PropertyMetadata(null));
	}
}
