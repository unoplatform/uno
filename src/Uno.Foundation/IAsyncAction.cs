using System.Threading.Tasks;

namespace Windows.Foundation
{
	public partial interface IAsyncAction : IAsyncInfo
	{
		AsyncActionCompletedHandler Completed { get; set; }

		void GetResults();
	}

	internal interface IAsyncActionInternal : IAsyncAction
	{
		Task Task { get; }
	}
}
