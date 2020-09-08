namespace Windows.Foundation
{
	public partial interface IAsyncAction : global::Windows.Foundation.IAsyncInfo
	{
		global::Windows.Foundation.AsyncActionCompletedHandler Completed
		{
			get;
			set;
		}

		void GetResults();
	}
}
