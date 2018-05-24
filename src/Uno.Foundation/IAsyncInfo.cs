namespace Windows.Foundation
{
	public  partial interface IAsyncInfo 
	{
		global::System.Exception ErrorCode
		{
			get;
		}

		uint Id
		{
			get;
		}

		global::Windows.Foundation.AsyncStatus Status
		{
			get;
		}

		void Cancel();

		void Close();
	}
}
