using System;
using System.Linq;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	// TODO Uno: IElementFactoryShim and its Microsoft.UI.Xaml.Controls ElementFactoryGetArgs/ElementFactoryRecycleArgs
	// are a WinUI 2 leftover (WinUI 2 could not rely on the platform's IElementFactory before RS5). WinUI 3 has none of
	// them: everything consumes Microsoft.UI.Xaml.IElementFactory, as Uno now does internally, and an IElementFactoryShim
	// is only adapted into one (ElementFactoryShimAdapter). Remove the interface and those args, and move
	// ElementFactory.GetElementCore/RecycleElementCore to the Microsoft.UI.Xaml args as in WinUI. This is a breaking change.
	public partial interface IElementFactoryShim
	{
		UIElement GetElement(ElementFactoryGetArgs args);
		void RecycleElement(ElementFactoryRecycleArgs args);
	}
}
