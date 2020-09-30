using System;
using Uno;

namespace Windows.Storage.Streams
{
	[Flags]
	public enum InputStreamOptions : uint
	{
		None = 0,

		[NotImplemented]
		Partial = 1,

		[NotImplemented]
		ReadAhead = 2,
	}
}
