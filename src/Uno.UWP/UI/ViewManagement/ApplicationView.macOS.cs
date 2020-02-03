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

		public string Title
		{
			get
			{
				if (NSApplication.SharedApplication.KeyWindow == null)
				{
					throw new InvalidOperationException($"{nameof(Title)} API must be used after KeyWindow is set");
				}
				return NSApplication.SharedApplication.KeyWindow.Title;
			}
			set
			{
				if (NSApplication.SharedApplication.KeyWindow == null)
				{
					throw new InvalidOperationException($"{nameof(Title)} API must be used after KeyWindow is set");
				}
				NSApplication.SharedApplication.KeyWindow.Title = value;
			}
		}
	}
}
#endif
