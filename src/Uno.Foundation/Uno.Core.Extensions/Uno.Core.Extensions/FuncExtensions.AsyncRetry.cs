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
#if !SILVERLIGHT || (SILVERLIGHT && WINDOWS_PHONE)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions
{
	internal static partial class FuncExtensions
	{
		public static async Task<T> Retry<T>(this Func<Task<T>> selector, int tries = 3, TimeSpan? retryDelay = null)
		{
			do
			{
				try
				{
					return await selector();
				}
				catch (Exception)
				{
					if (--tries == 0)
					{
						throw;
					}
				}

				await Task.Delay(retryDelay ?? TimeSpan.FromMilliseconds(100));
			}
			while (true);
		}

		public static async Task<T> Retry<T>(this Func<CancellationToken, Task<T>> selector, CancellationToken ct, int tries = 3, TimeSpan? retryDelay = null)
		{
			do
			{
				try
				{
					return await selector(ct);
				}
				catch (Exception)
				{
					if (--tries == 0)
					{
						throw;
					}
				}

				await Task.Delay(retryDelay ?? TimeSpan.FromMilliseconds(100), ct);
			}
			while (true);
		}

		public static async Task Retry(this Func<Task> action, int tries = 3, TimeSpan? retryDelay = null)
		{
			do
			{
				try
				{
					await action();
					return;
				}
				catch (Exception)
				{
					if (--tries == 0)
					{
						throw;
					}
				}

				await Task.Delay(retryDelay ?? TimeSpan.FromMilliseconds(100));
			}
			while (true);
		}

		public static async Task Retry(this Func<CancellationToken, Task> action, CancellationToken ct, int tries = 3, TimeSpan? retryDelay = null)
		{
			do
			{
				try
				{
					await action(ct);
					return;
				}
				catch (Exception)
				{
					if (--tries == 0)
					{
						throw;
					}
				}

				await Task.Delay(retryDelay ?? TimeSpan.FromMilliseconds(100), ct);
			}
			while (true);
		}
	}
}
#endif