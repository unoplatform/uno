#if !__SKIA__
#nullable enable

namespace Windows.UI.ViewManagement;

partial class InputPane
{
	private bool TryShowPlatform() => false;

	private bool TryHidePlatform() => false;
}
#endif
