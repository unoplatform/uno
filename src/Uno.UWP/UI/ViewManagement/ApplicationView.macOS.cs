#if __MACOS__
using System;
using System.Collections.Generic;
using System.Text;
using AppKit;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.ViewManagement
{
    partial class ApplicationView
	{
		internal void SetCoreBounds(NSWindow keyWindow, Foundation.Rect windowBounds)
		{
            VisibleBounds = windowBounds;

			if(this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Updated visible bounds {VisibleBounds}");
			}

			VisibleBoundsChanged?.Invoke(this, null);
		}
	}
}
#endif
