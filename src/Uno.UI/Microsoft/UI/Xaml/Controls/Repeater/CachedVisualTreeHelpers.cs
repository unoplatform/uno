using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class CachedVisualTreeHelpers
	{
		public static Rect GetLayoutSlot(FrameworkElement element)
			=> LayoutInformation.GetLayoutSlot(element);

		public static DependencyObject GetParent(DependencyObject child)
			=> VisualTreeHelper.GetParent(child);

		public static IDataTemplateComponent GetDataTemplateComponent(UIElement element)
			=> XamlBindingHelper.GetDataTemplateComponent(element);
	}
}
