namespace Windows.Services.Store
{
	/// <summary>
	/// Gets the result status for the rate and review request for the product.
	/// </summary>
	public enum StoreRateAndReviewStatus
	{
		/// <summary>
		/// The request was successful.
		/// </summary>
		Succeeded,

		/// <summary>
		/// The request was canceled by the user.
		/// </summary>
		CanceledByUser,

		/// <summary>
		/// The request encountered a network error.
		/// </summary>
		NetworkError,

		/// <summary>
		/// The request encountered an error.
		/// </summary>
		Error,
	}
}
