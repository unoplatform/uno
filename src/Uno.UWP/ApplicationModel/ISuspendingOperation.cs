using System;

namespace Windows.ApplicationModel
{
	public partial interface ISuspendingOperation
	{
		DateTimeOffset Deadline { get; }

		SuspendingDeferral GetDeferral();
	}
}
