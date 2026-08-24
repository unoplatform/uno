#nullable enable

using System;

#if __SKIA__

namespace Microsoft.UI.Xaml;

[Flags]
public enum ElementHighContrastAdjustment : uint
{
	None = 0,
	Application = 0x80000000,
	Auto = uint.MaxValue,
}

#endif
