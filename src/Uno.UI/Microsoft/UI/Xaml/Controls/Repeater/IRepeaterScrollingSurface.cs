using System;
using System.Linq;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Private.Controls
{
	internal delegate void ViewportChangedEventHandler(IRepeaterScrollingSurface sender, bool isFinal);

	internal delegate void PostArrangeEventHandler(IRepeaterScrollingSurface sender);

	internal delegate void ConfigurationChangedEventHandler(IRepeaterScrollingSurface sender);

	internal partial interface IRepeaterScrollingSurface
	{
		bool IsHorizontallyScrollable { get; }
		bool IsVerticallyScrollable { get; }
		UIElement AnchorElement { get; }
		event ConfigurationChangedEventHandler ConfigurationChanged;
		event PostArrangeEventHandler PostArrange;
		event ViewportChangedEventHandler ViewportChanged;
		void RegisterAnchorCandidate(UIElement element);
		void UnregisterAnchorCandidate(UIElement element);
		Windows.Foundation.Rect GetRelativeViewport(UIElement child);
	}
}
