#nullable enable

using System;

namespace Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Contains information about the DataRequested event. The system fires this
/// event when the user invokes the Share UI.
/// </summary>
public partial class DataRequestedEventArgs
{
	internal DataRequestedEventArgs(Action<DataRequest> deferralComplete)
	{
		Request = new DataRequest(deferralComplete);
	}

	/// <summary>
	/// Enables you to get the DataRequest object
	/// and either give it data or a failure message.
	/// </summary>
	public DataRequest Request { get; }
}
