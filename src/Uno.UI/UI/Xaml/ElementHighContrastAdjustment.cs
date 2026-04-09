using System;

namespace Microsoft.UI.Xaml
{
	[Flags]
	public enum ElementHighContrastAdjustment : uint
	{
		None = 0,
		Application = 2147483648,
		Auto = 4294967295,
	}
}
