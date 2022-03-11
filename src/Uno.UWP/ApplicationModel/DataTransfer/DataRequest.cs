#nullable enable

using System;
using Uno.Helpers;

namespace Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Lets your app supply the content the user wants to share or specify a message, if an error occurs.
/// </summary>
public partial class DataRequest
{
	internal DataRequest(Action<DataRequest> complete)
	{
		DeferralManager = new DeferralManager<DataRequestDeferral>(h => new DataRequestDeferral(h));
		DeferralManager.Completed += (s, e) => complete(this);
	}

	/// <summary>
	/// Sets or gets a DataPackage object that contains the content a user wants to share.
	/// </summary>
	public DataPackage Data { get; set; } = new DataPackage();

	/// <summary>
	/// Gets the deadline for finishing a delayed rendering operation. If execution goes beyond that deadline,
	/// the results of delayed rendering are ignored.
	/// </summary>
	public DateTimeOffset Deadline { get; }

	internal DeferralManager<DataRequestDeferral> DeferralManager { get; }

	/// <summary>
	/// Supports asynchronous sharing operations by creating and returning a DataRequestDeferral object.
	/// </summary>
	/// <returns>
	/// Supports asynchronous sharing operations by creating and returning a DataRequestDeferral object.
	/// </returns>
	public DataRequestDeferral GetDeferral() => DeferralManager.GetDeferral();
}
