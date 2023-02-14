using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Xaml.Islands;

namespace Microsoft.UI.Xaml.Hosting;

partial class DesktopWindowXamlSource
{
	partial void OnContentChangedPartial(XamlIslandRoot xamlIslandRoot)
	{
		// Ensure the root element of the XamlIsland is loaded.
		UIElement.LoadingRootElement(_root);
		UIElement.RootElementLoaded(_root);
	}
}
