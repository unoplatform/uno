using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Viewbox : global::Microsoft.UI.Xaml.FrameworkElement
	{
		partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue)
		{
			if (previousValue != null)
			{
				RemoveView(previousValue);
			}

			if (newValue != null)
			{
				AddView(newValue);
			}
		}
	}
}
