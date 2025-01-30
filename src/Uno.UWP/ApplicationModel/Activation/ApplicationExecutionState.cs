using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Activation;

/// <summary>
/// Specifies the execution state of the app.
/// </summary>
public enum ApplicationExecutionState
{
	/// <summary>
	/// The app is not running.
	/// </summary>
	NotRunning,

	/// <summary>
	/// The app is running.
	/// </summary>
	Running,

	/// <summary>
	/// The app is suspended.
	/// </summary>
	Suspended,

	/// <summary>
	/// The app was terminated after being suspended.
	/// </summary>
	Terminated,

	/// <summary>
	/// The app was closed by the user.
	/// </summary>
	ClosedByUser
}
