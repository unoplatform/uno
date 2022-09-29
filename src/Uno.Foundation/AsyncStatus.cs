#nullable disable

namespace Windows.Foundation
{
	public enum AsyncStatus
	{
		Canceled = 2,
		/// <summary>The operation has completed.</summary>
		Completed = 1,
		Error = 3,
		/// <summary>The operation has started.</summary>
		Started = 0
	}
}