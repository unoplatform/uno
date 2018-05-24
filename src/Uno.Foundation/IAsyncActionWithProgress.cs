using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Windows.Foundation
{
	public delegate void AsyncActionProgressHandler<TProgress>([In] IAsyncActionWithProgress<TProgress> asyncInfo, [In] TProgress progressInfo);
	public delegate void AsyncActionWithProgressCompletedHandler<TProgress>([In] IAsyncActionWithProgress<TProgress> asyncInfo, [In] AsyncStatus asyncStatus);

	public partial interface IAsyncActionWithProgress<TProgress> : IAsyncInfo
	{
		global::Windows.Foundation.AsyncActionProgressHandler<TProgress> Progress
		{
			get;
			set;
		}

		global::Windows.Foundation.AsyncActionWithProgressCompletedHandler<TProgress> Completed
		{
			get;
			set;
		}

		void GetResults();

	}
}
