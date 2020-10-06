using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Foundation
{
	public delegate void AsyncActionProgressHandler<TProgress>([In] IAsyncActionWithProgress<TProgress> asyncInfo, [In] TProgress progressInfo);
	public delegate void AsyncActionWithProgressCompletedHandler<TProgress>([In] IAsyncActionWithProgress<TProgress> asyncInfo, [In] AsyncStatus asyncStatus);

	public partial interface IAsyncActionWithProgress<TProgress> : IAsyncInfo
	{
		AsyncActionProgressHandler<TProgress> Progress { get; set; }

		AsyncActionWithProgressCompletedHandler<TProgress> Completed { get; set; }

		void GetResults();
	}

	internal interface IAsyncActionWithProgressInternal<TProgress> : IAsyncActionWithProgress<TProgress>
	{
		Task Task { get; }
	}
}
