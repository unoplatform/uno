#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation.Metadata;

namespace Windows.Foundation
{
	public delegate void AsyncOperationProgressHandler<TResult, TProgress>([In] IAsyncOperationWithProgress<TResult, TProgress> asyncInfo, [In] TProgress progressInfo);
	public delegate void AsyncOperationWithProgressCompletedHandler<TResult, TProgress>([In] IAsyncOperationWithProgress<TResult, TProgress> asyncInfo, [In] AsyncStatus asyncStatus);

	public partial interface IAsyncOperationWithProgress<TResult, TProgress> : IAsyncInfo
	{
	}
}

