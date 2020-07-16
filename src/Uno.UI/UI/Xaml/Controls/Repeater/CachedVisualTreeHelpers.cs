using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	internal class CachedVisualTreeHelpers
	{
		public static Rect GetLayoutSlot(FrameworkElement element)
			=> LayoutInformation.GetLayoutSlot(element);

		public static DependencyObject GetParent(DependencyObject child)
			=> VisualTreeHelper.GetParent(child);

		public static IDataTemplateComponent GetDataTemplateComponent(UIElement element)
			=> XamlBindingHelper.GetDataTemplateComponent(element);
	}
}
