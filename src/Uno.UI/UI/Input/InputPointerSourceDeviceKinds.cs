#if HAS_UNO_WINUI

using System;

namespace Microsoft.UI.Input
{
	[Flags]
	public enum InputPointerSourceDeviceKinds : uint
	{
		None = 0x0,
		Touch = 0x1,
		Pen = 0x2,
		Mouse = 0x4
	}
}
#endif
