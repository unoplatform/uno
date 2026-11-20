#nullable enable

using System;

namespace Windows.ApplicationModel.DataTransfer
{
	[Flags]
	public enum DataPackageOperation : uint
	{
		None = 0,
		Copy = 1,
		Move = 2,
		Link = 4,
		BackgroundTarget = 536870912,
		NewTarget = 1073741824,
	}
}
