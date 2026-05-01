#nullable enable

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using Windows.UI.Core;

namespace System
{
	public static partial class WindowsRuntimeSystemExtensions
	{
		public static IAsyncAction AsAsyncAction(this Task source)
			=> AsyncAction.FromTask(_ => source);

		public static IAsyncOperation<TResult> AsAsyncOperation<TResult>(this Task<TResult> source)
			=> new AsyncOperation<TResult>((_, __) => source);

		public static Task AsTask(this Task source)
			=> source;

		public static Task<T> AsTask<T>(this Task<T> source)
			=> source;

		public static Task AsTask(this Task source, CancellationToken ct)
			=> source;

		public static Task<T> AsTask<T>(this Task<T> source, CancellationToken ct)
			=> source;

		public static Task AsTask(this IAsyncAction source)
			=> source.AsTaskCore(CancellationToken.None);

		public static Task AsTask(this IAsyncAction source, CancellationToken cancellationToken)
			=> source.AsTaskCore(cancellationToken);

		private static Task AsTaskCore(this IAsyncAction source, CancellationToken ct)
		{
			if (source is IAsyncActionInternal action)
			{
				using var _ = ct.CanBeCanceled ? ct.Register(action.Cancel) : default;
				return action.Task;
			}

			if (source is Uno.UI.Dispatching.UIAsyncOperation operation)
			{
				return operation.AsTask(ct);
			}

			throw new NotSupportedException("Custom IAsyncAction implementations are not supported.");
		}

		public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source)
			=> source.AsTaskCore(CancellationToken.None);

		public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source, CancellationToken cancellationToken)
			=> source.AsTaskCore(cancellationToken);

		private static async Task<TResult> AsTaskCore<TResult>(this IAsyncOperation<TResult> source, CancellationToken ct)
		{
			if (source is IAsyncOperationInternal<TResult> operation)
			{
				using var _ = ct.CanBeCanceled ? ct.Register(operation.Cancel) : default;
				return await operation.Task;
			}

			throw new NotSupportedException("Custom IAsyncOperation implementations are not supported.");
		}

		public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source)
			=> source.AsTaskCore(CancellationToken.None);

		public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> progress)
			=> source.AsTaskCore(CancellationToken.None, progress);

		public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken)
			=> source.AsTaskCore(cancellationToken);

		public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
			=> source.AsTaskCore(cancellationToken, progress);

		private static async Task AsTaskCore<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken ct, IProgress<TProgress>? progress = null)
		{
			if (source is IAsyncActionWithProgressInternal<TProgress> operation)
			{
				using var _ = ct.CanBeCanceled ? ct.Register(operation.Cancel) : default;
				if (progress is { })
				{
					operation.Progress = (snd, p) => progress.Report(p);
				}
				await operation.Task;
			}

			throw new NotSupportedException("Custom IAsyncActionWithProgress implementations are not supported.");
		}

		public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
			=> source.AsTaskCore(CancellationToken.None);

		public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> progress)
			=> source.AsTaskCore(CancellationToken.None, progress);

		public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken)
			=> source.AsTaskCore(cancellationToken);

		public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
			=> source.AsTaskCore(cancellationToken, progress);

		private static async Task<TResult> AsTaskCore<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken ct, IProgress<TProgress>? progress = null)
		{
			if (source is IAsyncOperationWithProgressInternal<TResult, TProgress> operation)
			{
				using var _ = ct.CanBeCanceled ? ct.Register(operation.Cancel) : default;
				if (progress is { })
				{
					operation.Progress = (snd, p) => progress.Report(p);
				}
				return await operation.Task;
			}

			throw new NotSupportedException("Custom IAsyncActionWithProgress implementations are not supported.");
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TaskAwaiter GetAwaiter(this IAsyncAction source)
		{
			if (source is Uno.UI.Dispatching.UIAsyncOperation uiAsyncOperation)
			{
				return uiAsyncOperation.GetAwaiter();
			}
			else
			{
				return source.AsTask().GetAwaiter();
			}
		}
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TaskAwaiter<TResult> GetAwaiter<TResult>(this IAsyncOperation<TResult> source)
			=> source.AsTask().GetAwaiter();

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TaskAwaiter GetAwaiter<TProgress>(this IAsyncActionWithProgress<TProgress> source)
			=> source.AsTask().GetAwaiter();

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
			=> source.AsTask().GetAwaiter();
	}
}
