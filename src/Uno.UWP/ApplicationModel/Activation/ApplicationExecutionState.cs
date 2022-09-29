#nullable disable

using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.ApplicationModel.Activation
{
	public enum ApplicationExecutionState
	{
		NotRunning,
		Running,
		Suspended,
		Terminated,
		ClosedByUser
	}
}
