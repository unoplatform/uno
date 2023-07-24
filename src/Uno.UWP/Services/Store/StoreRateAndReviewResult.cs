#nullable enable

using System;

namespace Windows.Services.Store
{
	/// <summary>
	/// Provides response data for a request to rate and review the product.
	/// </summary>
	public partial class StoreRateAndReviewResult
	{
		internal StoreRateAndReviewResult(StoreRateAndReviewStatus status, Exception? extendedError = null)
		{
			Status = status;
			ExtendedError = extendedError;
		}

		/// <summary>
		/// Gets the status for the rate and review request for the product.
		/// </summary>
		public StoreRateAndReviewStatus Status { get; }

		/// <summary>
		/// Gets the error code for the request, if the operation encountered an error.
		/// </summary>
		public Exception? ExtendedError { get; }
	}
}
