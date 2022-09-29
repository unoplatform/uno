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
	}
}
