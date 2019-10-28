using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Uno.Extensions
{
	internal static class CoreDispatcherExtensions
	{
		/// <summary>
		/// Runs the specified <see cref="Task"/> on the a <see cref="CoreDispatcher"/>.
		/// </summary>
		/// <typeparam name="T">The result type</typeparam>
		/// <param name="dispatcher">The <see cref="CoreDispatcher"/> to run the task on</param>
		/// <param name="priority">The task execution priority</param>
		/// <param name="task">A function providing the task to execute</param>
		/// <returns>The result of the task's execution</returns>
		internal static async Task<T> RunWithResultAsync<T>(this CoreDispatcher dispatcher, CoreDispatcherPriority priority, Func<Task<T>> task)
		{
			var tcs = new TaskCompletionSource<T>();

			dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				async () =>
				{
					try
					{
						tcs.TrySetResult(await task());
					}
					catch (Exception e)
					{
						tcs.SetException(e);
					}
				});

			return await tcs.Task;
		}
	}
}
