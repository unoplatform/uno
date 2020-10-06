using System.Threading.Tasks;

namespace Windows.Foundation
{
	public partial interface IAsyncOperation<TResult> : IAsyncInfo
	{
		AsyncOperationCompletedHandler<TResult> Completed { get; set; }

		TResult GetResults();
	}

	internal interface IAsyncOperationInternal<TResult> : IAsyncOperation<TResult>
	{
		Task<TResult> Task { get; }
	}
}
