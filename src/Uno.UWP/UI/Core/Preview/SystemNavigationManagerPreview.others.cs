#nullable enable
#if !__MACOS__ && !__SKIA__

namespace Windows.UI.Core.Preview;

public partial class SystemNavigationManagerPreview
{
	// The constructor is not public in UWP/WinUI
	private SystemNavigationManagerPreview()
	{
	}

	internal bool HasConfirmedClose { get; private set; }

	internal bool RequestAppClose() { return false; }
}
#endif
