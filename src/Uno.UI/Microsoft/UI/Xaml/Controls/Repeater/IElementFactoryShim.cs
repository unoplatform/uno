using System;
using System.Linq;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public interface IElementFactoryShim
	{
		UIElement GetElement(ElementFactoryGetArgs args);
		void RecycleElement(ElementFactoryRecycleArgs args);
	}
}
