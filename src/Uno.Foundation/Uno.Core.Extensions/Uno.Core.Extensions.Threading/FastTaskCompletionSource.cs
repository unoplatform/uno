#nullable disable

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
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Threading
{
	/// <summary>
	/// This is a lightweight alternative to TaskCompletionSource.
	/// </summary>
	/// <remarks>
	/// In most situation, the TaskCompletionSource could be replaced directly by this one.
	/// It is slightly more efficient to use the .Task instead of directly awaiting this object.
	/// </remarks>
	internal class FastTaskCompletionSource<T> : INotifyCompletion
	{
		private int _terminationType; // 0-nom completed, 1-result, 2-exception, 3-canceled

		internal enum TerminationType
		{
			Running = 0,
			Result = 1,
			Exception = 2,
			Canceled = 3
		}

		private volatile bool _terminationSet;

		private ImmutableList<Action> _continuations;

		/// <summary>
		/// Current state of the object
		/// </summary>
		public TerminationType Termination => (TerminationType)_terminationType;

		/// <summary>
		/// If any termination has been set
		/// </summary>
		public bool IsCompleted => _terminationType != (int)TerminationType.Running;

		/// <summary>
		/// If SetCanceled has been called.
		/// </summary>
		/// <remarks>
		/// Calling SetException with a "TaskCanceledException" will produce the same result.
		/// </remarks>
		public bool IsCanceled => _terminationType == (int)TerminationType.Canceled;

		/// <summary>
		/// The capture of the original exception, if any.
		/// </summary>
		/// <remarks>
		/// Will be null until a SetException() has been called.
		/// NOTE: will be null if SetException is called with a "TaskCanceledException".
		/// </remarks>
		public ExceptionDispatchInfo ExceptionInfo { get; private set; }

		/// <summary>
		/// The result, if any.
		/// </summary>
		/// <remarks>
		/// Will be null until a SetResult() has been called.
		/// </remarks>
		public T Result { get; private set; }

		/// <summary>
		/// Get the captured exception (if any) - null if none or N/A
		/// </summary>
		/// <remarks>
		/// Will be null until a SetException() has been called.
		/// NOTE: will be null if SetException is called with a "TaskCanceledException".
		/// </remarks>
		public Exception Exception => ExceptionInfo?.SourceException;

		private static SpinWait _spin;

		/// <summary>
		/// Set the termination as "Canceled"
		/// </summary>
		/// <remarks>
		/// Will throw an InvalidOperationException if a termination has been set.
		/// Calling SetException with a "TaskCanceledException" will produce the same result.
		/// </remarks>
		public void SetCanceled()
		{
			if (!TrySetCanceled())
			{
				throw new InvalidOperationException("Already completed.");
			}
		}

		/// <summary>
		/// Set the termination as "Canceled"
		/// </summary>
		/// <remarks>
		/// Calling SetException with a "TaskCanceledException" will produce the same result.
		/// </remarks>
		public bool TrySetCanceled()
		{
			if (Interlocked.CompareExchange(ref _terminationType, (int)TerminationType.Canceled, (int)TerminationType.Running) != (int)TerminationType.Running)
			{
				return false;
			}

			RaiseOnCompleted();
			return true;
		}

		/// <summary>
		/// Set the termination on an exception
		/// </summary>
		/// <remarks>
		/// Will throw an InvalidOperationException if a termination has been set.
		/// Calling SetException with a "TaskCanceledException" will result as a if SetCanceled() has been called.
		/// </remarks>
		public void SetException(Exception exception)
		{
			if (!TrySetException(exception))
			{
				throw new InvalidOperationException("Already completed.");
			}
		}

		/// <summary>
		/// Set the termination on an exception wrapped in an ExceptionDispatchInfo
		/// </summary>
		/// <remarks>
		/// Will throw an InvalidOperationException if a termination has been set.
		/// Calling SetException with a "TaskCanceledException" will result as a if SetCanceled() has been called.
		/// </remarks>
		public void SetException(ExceptionDispatchInfo exceptionInfo)
		{
			if (!TrySetException(exceptionInfo))
			{
				throw new InvalidOperationException("Already completed.");
			}
		}

		/// <summary>
		/// Set the termination on an exception
		/// </summary>
		/// <remarks>
		/// Calling SetException with a "TaskCanceledException" will result as a if SetCanceled() has been called.
		/// </remarks>
		public bool TrySetException(Exception exception)
		{
			if (exception == null)
			{
				throw new ArgumentNullException(nameof(exception));
			}

			if (exception is TaskCanceledException)
			{
				return TrySetCanceled();
			}

			if (Interlocked.CompareExchange(ref _terminationType, (int)TerminationType.Exception, (int)TerminationType.Running) != (int)TerminationType.Running)
			{
				return false;
			}
			ExceptionInfo = ExceptionDispatchInfo.Capture(exception);
			_terminationSet = true;

			RaiseOnCompleted();
			return true;
		}

		/// <summary>
		/// Set the termination on an exception wrapped in an ExceptionDispatchInfo
		/// </summary>
		/// <remarks>
		/// Calling SetException with a "TaskCanceledException" will result as a if SetCanceled() has been called.
		/// </remarks>
		public bool TrySetException(ExceptionDispatchInfo exceptionInfo)
		{
			if (exceptionInfo == null)
			{
				throw new ArgumentNullException(nameof(exceptionInfo));
			}

			if (exceptionInfo.SourceException is TaskCanceledException)
			{
				return TrySetCanceled();
			}

			if (exceptionInfo.SourceException is OperationCanceledException)
			{
				return TrySetCanceled();
			}

			if (Interlocked.CompareExchange(ref _terminationType, (int)TerminationType.Exception, (int)TerminationType.Running) != (int)TerminationType.Running)
			{
				return false;
			}
			ExceptionInfo = exceptionInfo;
			_terminationSet = true;

			RaiseOnCompleted();
			return true;
		}

		/// <summary>
		/// Set the termination on a result
		/// </summary>
		/// <remarks>
		/// Will throw an InvalidOperationException if a termination has been set.
		/// </remarks>
		public void SetResult(T result)
		{
			if (!TrySetResult(result))
			{
				throw new InvalidOperationException("Already completed.");
			}
		}

		/// <summary>
		/// Set the termination on a result
		/// </summary>
		public bool TrySetResult(T result)
		{
			if (Interlocked.CompareExchange(ref _terminationType, (int)TerminationType.Result, (int)TerminationType.Running) != (int)TerminationType.Running)
			{
				return false;
			}

			Result = result;
			_terminationSet = true;

			RaiseOnCompleted();

			return true;
		}

		private void RaiseOnCompleted()
		{
			var continuations = Interlocked.Exchange(ref _continuations, null);

			if (continuations == null)
			{
				return; // nothing to do
			}

			foreach (var continuation in continuations)
			{
				continuation?.Invoke();
			}
		}

		/// <summary>
		/// GetResult or throw an exception, according to termination. BLOCKING: DON'T CALL THIS!
		/// </summary>
		/// <remarks>
		/// BLOCKING CALL!  Will wait until a termination has been set.
		/// The method is resigned for the "await" pattern, should not be called from code.
		/// </remarks>
		public T GetResult()
		{
			switch (Termination)
			{
				case TerminationType.Result:
					SpinUntilTermination();
					return Result;
				case TerminationType.Exception:
					SpinUntilTermination();
					ExceptionInfo?.Throw();
					return default(T)!; // will never reach here
				case TerminationType.Canceled:
					throw new OperationCanceledException();
				case TerminationType.Running:
				default:
					throw new InvalidOperationException("Still in progress.");
			}
		}

		/// <summary>
		/// "await" pattern implementation.
		/// </summary>
		public FastTaskCompletionSource<T> GetAwaiter()
		{
			return this;
		}

		/// <summary>
		/// "await" pattern implementation.
		/// </summary>

		public void OnCompleted(Action continuation)
		{
			Transactional.Update(
				ref _continuations,
				continuation,
				(previous, c) => previous == null ? ImmutableList<Action>.Empty.Add(c) : previous.Add(c));

			if (Termination != TerminationType.Running)
			{
				RaiseOnCompleted();
			}
		}

		private Task<T> _task;

		/// <summary>
		/// Task you can use to await for the result.
		/// </summary>
		public Task<T> Task
		{
			get
			{
				if (_task != null)
				{
					return _task;
				}

				if (_terminationType == 1) // Completed with a result ?
				{
					// Create an already-completed task (will result in sync behavior for await code)
					Interlocked.CompareExchange(ref _task, CreateSyncTask(), null);
				}
				else
				{
					// There's no way to create a "sync" Task for non-result terminations,
					// so we fallback to async mode in this case.

					// Create a waiting task for completion
					Interlocked.CompareExchange(ref _task, CreateAsyncTask(), null);
				}

				return _task;
			}
		}

		private Task<T> CreateSyncTask()
		{
			SpinUntilTermination();
			return System.Threading.Tasks.Task.FromResult(Result);
		}

		private void SpinUntilTermination()
		{
			while (!_terminationSet)
			{
				_spin.SpinOnce();
			}
		}

		private async Task<T> CreateAsyncTask()
		{
			return await this;
		}
	}
}
