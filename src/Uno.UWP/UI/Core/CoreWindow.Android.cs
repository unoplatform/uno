using System;
using Windows.Devices.Input;

namespace Windows.UI.Core;

internal sealed partial class CoreWindowExtension : ICoreWindowExtension
{
	public CoreCursor PointerCursor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public void ReleasePointerCapture(PointerIdentifier pointer) => throw new NotImplementedException();
	public void SetPointerCapture(PointerIdentifier pointer) => throw new NotImplementedException();
}
