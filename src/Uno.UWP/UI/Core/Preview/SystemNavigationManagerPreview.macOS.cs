#nullable enable

using AppKit;

namespace Windows.UI.Core.Preview;

public partial class SystemNavigationManagerPreview
{
	partial void CloseApp()
    {
		NSApplication.SharedApplication.KeyWindow.PerformClose(null);
	}
}
