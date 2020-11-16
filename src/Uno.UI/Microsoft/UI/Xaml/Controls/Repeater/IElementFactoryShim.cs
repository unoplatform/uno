using System;
using System.Linq;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial interface IElementFactoryShim
	{
		UIElement GetElement(ElementFactoryGetArgs args);
		void RecycleElement(ElementFactoryRecycleArgs args);
	}
}
