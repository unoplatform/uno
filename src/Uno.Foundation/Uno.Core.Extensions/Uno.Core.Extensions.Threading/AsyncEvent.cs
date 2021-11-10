#nullable enable

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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Threading
{
	[System.Diagnostics.DebuggerDisplayAttribute("Current Count = {_currentCount}")]
	public class AsyncEvent
	{
#if HAS_NO_CONCURRENT_DICT
		private Queue<FastTaskCompletionSource<bool>> _queue = new Queue<FastTaskCompletionSource<bool>>();
#else
		private ConcurrentQueue<FastTaskCompletionSource<bool>> _queue = new ConcurrentQueue<FastTaskCompletionSource<bool>>();
#endif
		private int _currentCount = 0;

		public AsyncEvent (int initialCount)
		{
			_currentCount = initialCount;
		}

		public int CurrentCount
		{
			get { return _currentCount; }
		}

		public Task<bool> Wait(CancellationToken cancellationToken)
		{
			var src = new FastTaskCompletionSource<bool>();

			bool shouldWait;
			int result;

			do
			{
				if(cancellationToken.IsCancellationRequested)
				{
					src.TrySetResult(false);
					return src.Task;
				}

				shouldWait = true;
				result = _currentCount;

				if (result > 0)
				{
					shouldWait = false;
				}
				else
				{
					break;
				}
			} while (Interlocked.CompareExchange(ref _currentCount, result - 1, result) != result);

			if(!shouldWait)
			{
				src.TrySetResult(true);
			}
			else
			{
#if HAS_NO_CONCURRENT_DICT
				lock(_queue)
#endif
				{
					_queue.Enqueue(src);
				}
			}

			return src.Task;
		}

		public void Release()
		{
			Release(1);
		}

		public void Release(int releaseCount)
		{
			int oldValue, newValue;
			do
			{
				oldValue = _currentCount;
				newValue = (_currentCount + releaseCount);

			} while (Interlocked.CompareExchange(ref _currentCount, newValue, oldValue) != oldValue);

			FastTaskCompletionSource<bool>? result;

			if (DequeueWaiter(out result))
			{
				result!.TrySetResult(true);
			}
		}

		private bool DequeueWaiter(out FastTaskCompletionSource<bool>? result)
		{
#if HAS_NO_CONCURRENT_DICT
			lock(_queue)
			{
				if(_queue.Count == 0)
				{
					result = null;
					return false;
				}

				result = _queue.Dequeue();
				return true;
			}
#else
			return _queue.TryDequeue(out result);
#endif
		}
	}
}
