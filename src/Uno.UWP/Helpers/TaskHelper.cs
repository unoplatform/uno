#nullable enable
using System;
using System.Threading.Tasks;

namespace Windows.Helpers;

internal static class TaskHelper
{
	// This is primarily to deal with the lack of ContinueWith on WASM
	public static void ContinueWith<T>(Task<T> task, Action<Exception> onException, Action<Task<T>> continuation)
	{
		async void Wait()
		{
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				onException(ex);
			}
			finally
			{
				continuation(task);
			}
		}

		Wait();
	}

	// This is primarily to deal with the lack of ContinueWith on WASM
	public static bool ResultOrContinueWith<T>(Task<T> task, Action<Exception> onException, Action<Task<T>> continuation, out T? result)
	{
		if (task.IsCompleted)
		{
			result = task.IsCompletedSuccessfully ? task.Result : default;
			return true;
		}
		else
		{
			result = default;
			ContinueWith(task, onException, continuation);
			return false;
		}
	}
}
