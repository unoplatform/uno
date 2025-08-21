using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal partial class ElementManager
{
	internal ElementManager(ITrackerHandleManager owner, bool useLayoutBounds = true)
	{
		m_owner = owner;
		m_useLayoutBounds = useLayoutBounds;
	}

	private bool m_useLayoutBounds;
	private readonly List<UIElement> m_realizedElements = new List<UIElement>(); // TODO MZ: These are tracked refs!
	private readonly List<Rect> m_realizedElementLayoutBounds = new List<Rect>();
	private int m_firstRealizedDataIndex = -1;
	private VirtualizingLayoutContext m_context;
}
