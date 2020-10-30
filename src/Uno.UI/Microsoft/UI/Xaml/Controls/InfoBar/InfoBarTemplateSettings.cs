// MUX reference InfoBarTemplateSettings.properties.cpp, commit 3125489

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class InfoBarTemplateSettings : DependencyObject
    {
		public InfoBarTemplateSettings()
		{
		}

		public IconElement IconElement
		{
			get => (IconElement)GetValue(IconElementProperty);
			set => SetValue(IconElementProperty, value);
		}

		public static readonly DependencyProperty IconElementProperty =
			DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(InfoBarTemplateSettings), new PropertyMetadata(null));
	}
}
