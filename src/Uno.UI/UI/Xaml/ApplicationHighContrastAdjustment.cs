#nullable enable

using System;

#if __SKIA__ || __NETSTD_REFERENCE__

namespace Microsoft.UI.Xaml;

[Flags]
public enum ApplicationHighContrastAdjustment : uint
{
	None = 0,
	Auto = uint.MaxValue,
}

#endif
