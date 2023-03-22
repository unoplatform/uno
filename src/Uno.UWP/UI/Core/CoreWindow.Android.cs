using System;
using Windows.Devices.Input;

namespace Windows.UI.Core;

internal sealed partial class CoreWindowExtension : ICoreWindowExtension
{
	public CoreCursor PointerCursor { get; set; } = new CoreCursor(CoreCursorType.Arrow, 0);

	public void ReleasePointerCapture(PointerIdentifier pointer) { }
	public void SetPointerCapture(PointerIdentifier pointer) { }
}
