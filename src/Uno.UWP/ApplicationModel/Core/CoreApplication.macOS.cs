using AppKit;

namespace Windows.ApplicationModel.Core;

partial class CoreApplication
{
	private static void ExitPlatform() => NSApplication.SharedApplication.Terminate(null);
}
