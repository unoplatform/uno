using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TabViewItemTemplateSettings
    {
		public IconElement IconElement
		{
			get => (IconElement)GetValue(IconElementProperty);
			set => SetValue(IconElementProperty, value);
		}

		public static readonly DependencyProperty IconElementProperty { get; } =
			DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(TabViewItemTemplateSettings), new PropertyMetadata(null));
	}
}
