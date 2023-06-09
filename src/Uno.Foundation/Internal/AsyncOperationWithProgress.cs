using Uno;

namespace Windows.Foundation;

internal static class AsyncOperationWithProgress
{
	public static AsyncOperationWithProgress<TResult, TProgress> FromFuncAsync<TResult, TProgress>(FuncAsync<AsyncOperationWithProgress<TResult, TProgress>, TResult> builder) =>
		new AsyncOperationWithProgress<TResult, TProgress>(builder);
}
