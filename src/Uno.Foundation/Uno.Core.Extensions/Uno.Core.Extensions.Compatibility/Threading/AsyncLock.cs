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
using System.Threading;
using System.Threading.Tasks;

using Uno.Disposables;

namespace Uno.Threading
{
	/// <summary>
	/// An asynchronous lock, that can be used in conjunction with C# async/await
	/// </summary>
	internal sealed class AsyncLock
	{
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Acquires the lock, then provides a disposable to release it.
		/// </summary>
		/// <param name="ct">A cancellation token to cancel the lock</param>
		/// <returns>An IDisposable instance that allows the release of the lock.</returns>
		public async Task<IDisposable> LockAsync(CancellationToken ct)
		{
			await _semaphore.WaitAsync(ct);

			return Disposable.Create(() => _semaphore.Release());
		}
	}
}
