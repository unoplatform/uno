#nullable enable

namespace Windows.UI.Xaml.Controls
{
	public partial class IconSource : global::Windows.UI.Xaml.DependencyObject
	{
		internal virtual IconElement? CreateIconElement()
			=> default;
	}
}
