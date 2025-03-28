using System;
using System.Linq;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial interface IElementFactoryShim
	{
		UIElement GetElement(ElementFactoryGetArgs args);
		void RecycleElement(ElementFactoryRecycleArgs args);
	}
}
