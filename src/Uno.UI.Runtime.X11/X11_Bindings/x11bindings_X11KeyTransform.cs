#nullable enable

using System;
using Windows.System;
using Uno.UI.Runtime.Linux;

namespace Uno.UI.Runtime.X11;

public static class X11KeyTransform
{
	public static VirtualKey VirtualKeyFromKeySym(IntPtr key) => LinuxKeyMap.FromKeysym(unchecked((uint)key.ToInt64()));
}
