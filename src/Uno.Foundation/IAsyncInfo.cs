using System;
using System.Threading.Tasks;

namespace Windows.Foundation
{
	public partial interface IAsyncInfo 
	{
		Exception ErrorCode { get; }

		uint Id { get; }

		AsyncStatus Status { get; }

		void Cancel();

		void Close();
	}
}
