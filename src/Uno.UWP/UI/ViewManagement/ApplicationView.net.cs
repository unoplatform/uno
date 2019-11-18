#if NET461
using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;

namespace Windows.UI.ViewManagement
{
    partial class ApplicationView
	{
		internal IDisposable SetVisibleBounds(Rect newVisibleBounds)
		{
			var old = VisibleBounds;
			VisibleBounds = newVisibleBounds;

			return new DisposableAction(() => VisibleBounds = old);
		}
	}
}
#endif
