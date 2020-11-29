#nullable enable

using System;

namespace Windows.ApplicationModel.DataTransfer
{
	// only one operation is supported - copy; 'none' is default value in DataPackage
	[Flags]
	public enum DataPackageOperation : uint
	{
		None = 0,

		Copy = 1,
		
		// two options without support
		[global::Uno.NotImplemented]
		Move = 2,

		[global::Uno.NotImplemented]
		Link = 4,
	}
}
