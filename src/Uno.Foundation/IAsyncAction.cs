namespace Windows.Foundation
{
	public partial interface IAsyncAction : IAsyncInfo
	{
		AsyncActionCompletedHandler Completed { get; set; }

		void GetResults();
	}
}
