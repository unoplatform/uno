namespace Windows.Foundation
{
	public partial interface IAsyncOperation<TResult> : IAsyncInfo
	{
		AsyncOperationCompletedHandler<TResult> Completed { get; set; }

		TResult GetResults();
	}
}
