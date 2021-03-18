#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
#pragma warning disable 67

using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.UI.ViewManagement
{
	public interface IApplicationViewSpanningRects
	{
		IReadOnlyList<Rect> GetSpanningRects();
	}
}
