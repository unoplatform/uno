using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	public partial class Viewbox : FrameworkElement
	{
		partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue)
		{
			previousValue?.RemoveFromSuperview();

			if (newValue != null)
			{
				AddSubview(newValue);
			}
		}
	}
}
