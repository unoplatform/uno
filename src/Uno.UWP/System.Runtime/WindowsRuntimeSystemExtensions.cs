
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using Windows.UI.Core;

namespace System
{
	public static class WindowsRuntimeSystemExtensions
	{
		public static IAsyncAction AsAsyncAction(this Task source) => AsyncAction.FromTask(_ => source);
		public static IAsyncOperation<TResult> AsAsyncOperation<TResult>(this Task<TResult> source) => new AsyncOperation<TResult>(_ => source);
		public static Task AsTask(this IAsyncAction source) => source.AsTask(CancellationToken.None);
		public static Task AsTask(this Task source) => source;
		public static Task<T> AsTask<T>(this Task<T> source) => source;
		public static Task AsTask(this Task source, CancellationToken ct) => source;
		public static Task<T> AsTask<T>(this Task<T> source, CancellationToken ct) => source;

		public static Task AsTask(this IAsyncAction source, CancellationToken cancellationToken)
		{
			if (source is AsyncAction action)
			{

				cancellationToken.Register(() => action.Cancel());

				return action.Task;
			}

			throw new NotSupportedException("Custom IAsyncAction implementations are not supported.");
		}

		public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source) => ((AsyncOperation<TResult>)source).Task;
		[NotImplemented]
		public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source) { throw new NotImplementedException(); }
		[NotImplemented]
		public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> progress) { throw new NotImplementedException(); }
		[NotImplemented]
		public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken) { throw new NotImplementedException(); }
		public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source, CancellationToken cancellationToken)
		{
			if (source is AsyncOperation<TResult> operation)
			{
				cancellationToken.Register(() => operation.Cancel());

				return operation.Task;
			}

			throw new NotSupportedException("Custom IAsyncOperation implementations are not supported.");
		}

		[NotImplemented]
		public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress) { throw new NotImplementedException(); }
		[NotImplemented]
		public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source) { throw new NotImplementedException(); }
		[NotImplemented]
		public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> progress) { throw new NotImplementedException(); }
		[NotImplemented]
		public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken) { throw new NotImplementedException(); }
		[NotImplemented]
		public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress) { throw new NotImplementedException(); }
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TaskAwaiter GetAwaiter(this IAsyncAction source)
		{
			if (source is UIAsyncOperation uiAsyncOperation)
			{
				return uiAsyncOperation.GetAwaiter();
			}
			else if (source is AsyncAction asyncAction)
			{
				return asyncAction.Task.GetAwaiter();
			}
			else
			{
			  throw new NotSupportedException("Custom IAsyncOperation implementations are not supported.");
			}
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TaskAwaiter<TResult> GetAwaiter<TResult>(this IAsyncOperation<TResult> source) => ((AsyncOperation<TResult>)source).Task.GetAwaiter();
		[EditorBrowsable(EditorBrowsableState.Never)]
		[NotImplemented]
		public static TaskAwaiter GetAwaiter<TProgress>(this IAsyncActionWithProgress<TProgress> source) { throw new NotImplementedException(); }
		[EditorBrowsable(EditorBrowsableState.Never)]
		[NotImplemented]
		public static TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source) { throw new NotImplementedException(); }
	}
}
