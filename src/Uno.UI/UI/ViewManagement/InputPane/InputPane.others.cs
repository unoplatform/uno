#if !__ANDROID__ && !__APPLE_UIKIT__ && !__SKIA__
#nullable enable

namespace Windows.UI.ViewManagement;

partial class InputPane
{
	private bool TryShowPlatform() => false;

	private bool TryHidePlatform() => false;
}
#endif
