using System;

namespace Windows.System.Threading
{
	[Flags]
	public enum WorkItemOptions
	{
		None = 0,
		TimeSliced = 1
	}
}
