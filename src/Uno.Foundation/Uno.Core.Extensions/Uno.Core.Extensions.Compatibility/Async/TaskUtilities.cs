// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if SILVERLIGHT && !WINPRT
using StaticTask = System.Threading.Tasks.TaskEx;
#else
using StaticTask = System.Threading.Tasks.Task;
#endif

namespace Uno
{
	internal static class TaskUtilities
	{
		/// <summary>
		/// Executes a provided <see cref="Task"/>, cancelling the task and executing a fallback action if the Task doesn't complete before the provided timeout.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="taskSelector"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public static async Task Timeout(CancellationToken ct, Func<CancellationToken, Task> taskSelector, TimeSpan timeout, Action onTimedOut)
		{
			// We're not using CancellationTokenSource's timeout support because we want to be able to trace code and know when it's
			// about to be cancelled because of a timeout.
			using (var tokenSource = new CancellationTokenSource())
			{
				var task = taskSelector(tokenSource.Token);

				if ((await StaticTask.WhenAny(task, StaticTask.Delay(timeout, ct))) != task)
				{
					// We timed out, cancel the task and throw a relevant exception.
					tokenSource.Cancel();
					onTimedOut();
				}
			}
		}

		/// <summary>
		/// Executes a provided <see cref="Task"/>, cancelling the task and throwing a <see cref="TimeoutException"/> if the Task doesn't complete before the provided timeout.
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="taskSelector"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public static Task Timeout(CancellationToken ct, Func<CancellationToken, Task> taskSelector, TimeSpan timeout)
		{
			return TaskUtilities.Timeout(
				ct,
				taskSelector,
				timeout,
				() => { throw new TimeoutException("A Task timed out."); });
		}

		/// <summary>
		/// Executes a provided <see cref="Task"/>, cancelling the task and executing a special selector if the Task doesn't complete before the provided timeout.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="ct"></param>
		/// <param name="taskSelector"></param>
		/// <param name="timeout"></param>
		/// <param name="timedOutValueSelector"></param>
		/// <returns></returns>
		public static async Task<T> Timeout<T>(CancellationToken ct, Func<CancellationToken, Task<T>> taskSelector, TimeSpan timeout, Func<T> timedOutValueSelector)
		{
			// We're not using CancellationTokenSource's timeout support because we want to be able to trace code and know when it's
			// about to be cancelled because of a timeout.
			using (var tokenSource = new CancellationTokenSource())
			{
				var task = taskSelector(tokenSource.Token);

				if ((await StaticTask.WhenAny(task, StaticTask.Delay(timeout, ct))) != task)
				{
					// We timed out, cancel the task and throw a relevant exception.
					tokenSource.Cancel();
					return timedOutValueSelector();
				}

				return await task;
			}
		}

		/// <summary>
		/// Executes a provided <see cref="Task"/>, cancelling the task and throwing a <see cref="TimeoutException"/> if the Task doesn't complete before the provided timeout.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="ct"></param>
		/// <param name="taskSelector"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public static Task<T> Timeout<T>(CancellationToken ct, Func<CancellationToken, Task<T>> taskSelector, TimeSpan timeout)
		{
			return TaskUtilities.Timeout<T>(
				ct,
				taskSelector,
				timeout,
				() => { throw new TimeoutException("A Task timed out."); });
		}
	}
}
