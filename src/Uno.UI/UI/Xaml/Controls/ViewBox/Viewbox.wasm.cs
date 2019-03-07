using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	public  partial class Viewbox : global::Windows.UI.Xaml.FrameworkElement
	{
		partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue)
		{
			if (previousValue != null)
			{
				RemoveChild(previousValue);
			}

			AddChild(newValue);
		}
	}
}
