// Windows Community Toolkit
// Copyright © .NET Foundation and Contributors
// All rights reserved.
// ## MIT License (MIT)
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.System;

#nullable enable

namespace Uno.UI.RuntimeTests.Extensions
{
    /// <summary>
    /// Helpers for executing code in a <see cref="DispatcherQueue"/>.
    /// </summary>
    public static class DispatcherQueueExtensions
    {
        /// <summary>
        /// Indicates whether or not <see cref="DispatcherQueue.HasThreadAccess"/> is available.
        /// </summary>
        private static readonly bool IsHasThreadAccessPropertyAvailable = ApiInformation.IsMethodPresent("Windows.System.DispatcherQueue", "HasThreadAccess");

        /// <summary>
        /// Invokes a given function on the target <see cref="DispatcherQueue"/> and returns a
        /// <see cref="Task"/> that completes when the invocation of the function is completed.
        /// </summary>
        /// <param name="dispatcher">The target <see cref="DispatcherQueue"/> to invoke the code on.</param>
        /// <param name="function">The <see cref="Action"/> to invoke.</param>
        /// <param name="priority">The priority level for the function to invoke.</param>
        /// <returns>A <see cref="Task"/> that completes when the invocation of <paramref name="function"/> is over.</returns>
        /// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task EnqueueAsync(this DispatcherQueue dispatcher, Action function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            // Run the function directly when we have thread access.
            // Also reuse Task.CompletedTask in case of success,
            // to skip an unnecessary heap allocation for every invocation.
            if (IsHasThreadAccessPropertyAvailable && dispatcher.HasThreadAccess)
            {
                try
                {
                    function();

                    return Task.CompletedTask;
                }
                catch (Exception e)
                {
                    return Task.FromException(e);
                }
            }

            static Task TryEnqueueAsync(DispatcherQueue dispatcher, Action function, DispatcherQueuePriority priority)
            {
                var taskCompletionSource = new TaskCompletionSource<object?>();

                if (!dispatcher.TryEnqueue(priority, () =>
                {
                    try
                    {
                        function();

                        taskCompletionSource.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }))
                {
                    taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
                }

                return taskCompletionSource.Task;
            }

            return TryEnqueueAsync(dispatcher, function, priority);
        }

        /// <summary>
        /// Invokes a given function on the target <see cref="DispatcherQueue"/> and returns a
        /// <see cref="Task{TResult}"/> that completes when the invocation of the function is completed.
        /// </summary>
        /// <typeparam name="T">The return type of <paramref name="function"/> to relay through the returned <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="dispatcher">The target <see cref="DispatcherQueue"/> to invoke the code on.</param>
        /// <param name="function">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <param name="priority">The priority level for the function to invoke.</param>
        /// <returns>A <see cref="Task"/> that completes when the invocation of <paramref name="function"/> is over.</returns>
        /// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> EnqueueAsync<T>(this DispatcherQueue dispatcher, Func<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            if (IsHasThreadAccessPropertyAvailable && dispatcher.HasThreadAccess)
            {
                try
                {
                    return Task.FromResult(function());
                }
                catch (Exception e)
                {
                    return Task.FromException<T>(e);
                }
            }

            static Task<T> TryEnqueueAsync(DispatcherQueue dispatcher, Func<T> function, DispatcherQueuePriority priority)
            {
                var taskCompletionSource = new TaskCompletionSource<T>();

                if (!dispatcher.TryEnqueue(priority, () =>
                {
                    try
                    {
                        taskCompletionSource.SetResult(function());
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }))
                {
                    taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
                }

                return taskCompletionSource.Task;
            }

            return TryEnqueueAsync(dispatcher, function, priority);
        }

        /// <summary>
        /// Invokes a given function on the target <see cref="DispatcherQueue"/> and returns a
        /// <see cref="Task"/> that acts as a proxy for the one returned by the given function.
        /// </summary>
        /// <param name="dispatcher">The target <see cref="DispatcherQueue"/> to invoke the code on.</param>
        /// <param name="function">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <param name="priority">The priority level for the function to invoke.</param>
        /// <returns>A <see cref="Task"/> that acts as a proxy for the one returned by <paramref name="function"/>.</returns>
        /// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task EnqueueAsync(this DispatcherQueue dispatcher, Func<Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            // If we have thread access, we can retrieve the task directly.
            // We don't use ConfigureAwait(false) in this case, in order
            // to let the caller continue its execution on the same thread
            // after awaiting the task returned by this function.
            if (IsHasThreadAccessPropertyAvailable && dispatcher.HasThreadAccess)
            {
                try
                {
                    if (function() is Task awaitableResult)
                    {
                        return awaitableResult;
                    }

                    return Task.FromException(GetEnqueueException("The Task returned by function cannot be null."));
                }
                catch (Exception e)
                {
                    return Task.FromException(e);
                }
            }

            static Task TryEnqueueAsync(DispatcherQueue dispatcher, Func<Task> function, DispatcherQueuePriority priority)
            {
                var taskCompletionSource = new TaskCompletionSource<object?>();

                if (!dispatcher.TryEnqueue(priority, async () =>
                {
                    try
                    {
                        if (function() is Task awaitableResult)
                        {
                            await awaitableResult.ConfigureAwait(false);

                            taskCompletionSource.SetResult(null);
                        }
                        else
                        {
                            taskCompletionSource.SetException(GetEnqueueException("The Task returned by function cannot be null."));
                        }
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }))
                {
                    taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
                }

                return taskCompletionSource.Task;
            }

            return TryEnqueueAsync(dispatcher, function, priority);
        }

        /// <summary>
        /// Invokes a given function on the target <see cref="DispatcherQueue"/> and returns a
        /// <see cref="Task{TResult}"/> that acts as a proxy for the one returned by the given function.
        /// </summary>
        /// <typeparam name="T">The return type of <paramref name="function"/> to relay through the returned <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="dispatcher">The target <see cref="DispatcherQueue"/> to invoke the code on.</param>
        /// <param name="function">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <param name="priority">The priority level for the function to invoke.</param>
        /// <returns>A <see cref="Task{TResult}"/> that relays the one returned by <paramref name="function"/>.</returns>
        /// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> EnqueueAsync<T>(this DispatcherQueue dispatcher, Func<Task<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            if (IsHasThreadAccessPropertyAvailable && dispatcher.HasThreadAccess)
            {
                try
                {
                    if (function() is Task<T> awaitableResult)
                    {
                        return awaitableResult;
                    }

                    return Task.FromException<T>(GetEnqueueException("The Task returned by function cannot be null."));
                }
                catch (Exception e)
                {
                    return Task.FromException<T>(e);
                }
            }

            static Task<T> TryEnqueueAsync(DispatcherQueue dispatcher, Func<Task<T>> function, DispatcherQueuePriority priority)
            {
                var taskCompletionSource = new TaskCompletionSource<T>();

                if (!dispatcher.TryEnqueue(priority, async () =>
                {
                    try
                    {
                        if (function() is Task<T> awaitableResult)
                        {
                            var result = await awaitableResult.ConfigureAwait(false);

                            taskCompletionSource.SetResult(result);
                        }
                        else
                        {
                            taskCompletionSource.SetException(GetEnqueueException("The Task returned by function cannot be null."));
                        }
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }))
                {
                    taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
                }

                return taskCompletionSource.Task;
            }

            return TryEnqueueAsync(dispatcher, function, priority);
        }

        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> to return when an enqueue operation fails.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <returns>An <see cref="InvalidOperationException"/> with a specified message.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidOperationException GetEnqueueException(string message)
        {
            return new InvalidOperationException(message);
        }
    }
}
