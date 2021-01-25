// MUX Reference NavigationViewItemPresenter.properties.cpp, commit de78834

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class NavigationViewItemPresenter
    {
		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public static DependencyProperty IconProperty { get; } =
			DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(NavigationViewItemPresenter), new PropertyMetadata(null));
	}
}
