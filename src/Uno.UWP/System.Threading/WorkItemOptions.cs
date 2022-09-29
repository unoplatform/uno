#nullable disable

using System;

namespace Windows.System.Threading
{
	/// <summary>
	/// Specifies how work items should be run.
	/// </summary>
	[Flags]
	public enum WorkItemOptions : uint
	{
		/// <summary>
		/// The work item should be run when the thread pool has an available worker thread.
		/// </summary>
		None = 0U,
		/// <summary>
		/// The work items should be run simultaneously with other work items sharing a processor.
		/// </summary>
		TimeSliced = 1U
	}
}
