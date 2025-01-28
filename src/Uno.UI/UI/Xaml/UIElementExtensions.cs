#if !__ANDROID__ && !__APPLE_UIKIT__
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml
{
	public static partial class UIElementExtensions
	{
		/// <summary>
		/// Get the parent view in the visual tree.
		/// </summary>
		public static UIElement GetVisualTreeParent(this UIElement uiElement) => (uiElement as FrameworkElement)?.VisualParent;
	}
}
#endif
