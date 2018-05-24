namespace Windows.Foundation
{
	public  partial interface IAsyncOperation<TResult> : global::Windows.Foundation.IAsyncInfo
	{
		AsyncOperationCompletedHandler<TResult> Completed
		{
			get;
			set;
		}

		TResult GetResults();
	}
}
