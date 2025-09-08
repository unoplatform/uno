using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	[Flags]
	public enum ElementRealizationOptions : uint
	{
		None = 0,
		ForceCreate = 1,
		SuppressAutoRecycle = 2
	}
}
