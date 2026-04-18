using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	public static partial class AsyncInfo
	{
		[Uno.NotImplemented]
		public static IAsyncAction Run(Func<CancellationToken, Task> taskProvider) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static IAsyncActionWithProgress<TProgress> Run<TProgress>(Func<CancellationToken, IProgress<TProgress>, Task> taskProvider) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static IAsyncOperation<TResult> Run<TResult>(Func<CancellationToken, Task<TResult>> taskProvider) => AsyncOperation.FromTask(taskProvider);
		[Uno.NotImplemented]
		public static IAsyncOperationWithProgress<TResult, TProgress> Run<TResult, TProgress>(Func<CancellationToken, IProgress<TProgress>, Task<TResult>> taskProvider) { throw new NotImplementedException(); }
	}
}
