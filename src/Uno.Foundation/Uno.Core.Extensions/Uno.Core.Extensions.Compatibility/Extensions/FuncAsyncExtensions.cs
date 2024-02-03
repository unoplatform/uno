// ******************************************************************
// Copyright � 2015-2018 Uno Platform Inc. All rights reserved.
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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Collections;
using Uno.Threading;

namespace Uno.Extensions
{
	/// <summary>
	/// Extensions of <see cref="FuncAsync"/>.
	/// </summary>
	internal static class FuncAsyncExtensions
	{
		/// <summary>
		/// Prevents parallel execution of the FuncAsync
		/// </summary>
		/// <param name="func">Func to lock</param>
		/// <param name="mode">Mode to use for locking</param>
		/// <returns>A FuncAsync which cannot have multiple instance running at a same time</returns>
		public static FuncAsync<TResult> LockInvocation<TResult>(this FuncAsync<TResult> func, InvocationLockingMode mode = InvocationLockingMode.Share)
		{
			// Note: Do not use TaskCompletionSource, for strange reasons it cause app crashes on iOS (on SetException).
			// Prefer keep task by themselves instead of trying to replicate task state to a TaskCompletionSource.

			if (mode == InvocationLockingMode.Share)
			{
				var gate = new object();
				var pending = default(Task<TResult>);

				return async ct =>
				{
					var created = false;
					try
					{
						var task = pending;
						if (task == null)
						{
							lock (gate)
							{
								task = pending;
								if (task == null)
								{
									created = true;
									task = pending = func(ct);
								}
							}
						}

						return await task;
					}
					finally
					{
						// Note: Keep trace of the creation and let to the initiator the responsibility to removed the task from pendings
						// DO NOT auto remove at the end of the task itself: If the task run synchronously, we might TryRemove the task before it is actually added to the pendings.
						if (created)
						{
							pending = null;
						}
					}
				};
			}
			else
			{
				var gate = new AsyncLock();

				return async ct =>
				{
					using (await gate.LockAsync(ct))
					{
						return await func(ct);
					}
				};
			}
		}

		/// <summary>
		/// Prevents parallel execution of the FuncAsync for a SAME PARAMETER
		/// </summary>
		/// <param name="func">Func to lock</param>
		/// <param name="mode">Mode to use for locking FOR A SAME PARAMETER</param>
		/// <returns>A FuncAsync which cannot have multiple instance running at a same time</returns>
		public static FuncAsync<TParam, TResult> LockInvocation<TParam, TResult>(this FuncAsync<TParam, TResult> func, InvocationLockingMode mode = InvocationLockingMode.Share)
			where TParam : class
		{
			// Note: Do not use TaskCompletionSource, for strange reasons it cause app crashes on iOS (on SetException).
			// Prefer keep task by themselves instead of trying to replicate task state to a TaskCompletionSource.

			if (mode == InvocationLockingMode.Share)
			{
				var pendings = new System.Collections.Concurrent.ConcurrentDictionary<TParam, Task<TResult>>();

				return async (ct, param) =>
				{
					var created = false;
					try
					{
						return await pendings.GetOrAdd(param, p =>
						{
							created = true;
							return func(ct, p);
						});
					}
					finally
					{
						// Note: Keep trace of the creation and let to the initiator the responsibility to removed the task from pendings
						// DO NOT auto remove at the end of the task itself: If the task run synchronously, we might TryRemove the task before it is actually added to the pendings.
						if (created)
						{
							Task<TResult> _;
							pendings.TryRemove(param, out _);
						}
					}
				};
			}
			else
			{
				var gates = new ConditionalWeakTable<TParam, AsyncLock>();

				return async (ct, param) =>
				{
					var gate = gates.GetValue(param, _ => new AsyncLock());
					using (await gate.LockAsync(ct))
					{
						return await func(ct, param);
					}
				};
			}
		}
	}
}
