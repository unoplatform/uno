using System;
using System.Collections.Generic;
using System.Text;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Composition
{
	class UIElementExtensions
	{
		public static ContainerVisual GetVisual(this Windows.UI.Xaml.UIElement element)
		{
			var hostVisual = ElementCompositionPreview.GetElementVisual(element);
			var root = hostVisual.Compositor.CreateContainerVisual();
			ElementCompositionPreview.SetElementChildVisual(element, root);
			return root;
		}
	}
}
