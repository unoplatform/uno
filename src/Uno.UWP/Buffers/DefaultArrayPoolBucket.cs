// #define TRACE_REUSE
//
// Imported from https://github.com/dotnet/corefx/commit/d9d1e815ad6c642cf5d61afa4a16726548598bb2 until Xamarin exposes it properly.
// Trimming portions imported from https://github.com/dotnet/runtime/blob/f53c8dcd130e7591079e9475fb0a3a22c3f21adc/src/libraries/System.Private.CoreLib/src/System/Buffers/TlsOverPerCoreLockedStacksArrayPool.cs#L412
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.System;

namespace Uno.Buffers
{
	internal sealed partial class ArrayPool<T>
	{
		/// <summary>Provides a thread-safe bucket containing buffers that can be Rent'd and Return'd.</summary>
		private sealed class Bucket
		{
			internal readonly int _bufferLength;
			private readonly T[]?[] _buffers;
			private readonly int _poolId;
			private static readonly int SizeOfT = Unsafe.SizeOf<T>();

			private SpinLock _lock; // do not make this readonly; it's a mutable struct
			private int _index;
			private TimeSpan _timestamp;
			private int _count;

#if TRACE_REUSE
			private int _created;
			private int _reused;
#endif

			/// <summary>
			/// Creates the pool with numberOfBuffers arrays where each buffer is of bufferLength length.
			/// </summary>
			internal Bucket(int bufferLength, int numberOfBuffers, int poolId)
			{
				_lock = new SpinLock(Debugger.IsAttached); // only enable thread tracking if debugger is attached; it adds non-trivial overheads to Enter/Exit
				_buffers = new T[numberOfBuffers][];
				_bufferLength = bufferLength;
				_poolId = poolId;
			}

			/// <summary>Gets an ID for the bucket to use with events.</summary>
			internal int Id => GetHashCode();

			/// <summary>Takes an array from the bucket.  If the bucket is empty, returns null.</summary>
			internal T[]? Rent()
			{
				T[]?[] buffers = _buffers;
				T[]? buffer = null;

				// While holding the lock, grab whatever is at the next available index and
				// update the index.  We do as little work as possible while holding the spin
				// lock to minimize contention with other threads.  The try/finally is
				// necessary to properly handle thread aborts on platforms which have them.
				bool lockTaken = false, allocateBuffer = false;

				try
				{
					_lock.Enter(ref lockTaken);

					if (_index < buffers.Length)
					{
						buffer = buffers[_index];
						buffers[_index++] = null;

						if (buffer == null)
						{
							allocateBuffer = true;
						}
						else
						{
							_count--;
#if TRACE_REUSE
							_reused++;
#endif
						}
					}
				}
				finally
				{
					if (lockTaken) _lock.Exit(false);
				}

				// While we were holding the lock, we grabbed whatever was at the next available index, if
				// there was one.  If we tried and if we got back null, that means we hadn't yet allocated
				// for that slot, in which case we should do so now.
				if (allocateBuffer)
				{
					buffer = new T[_bufferLength];
#if TRACE_REUSE
					_created++;
#endif
				}

				return buffer;
			}

			private void TrimOne()
			{
				T[]?[] buffers = _buffers;

				// While holding the lock, grab whatever is at the next available index and
				// update the index.  We do as little work as possible while holding the spin
				// lock to minimize contention with other threads.  The try/finally is
				// necessary to properly handle thread aborts on platforms which have them.
				bool lockTaken = false;

				try
				{
					_lock.Enter(ref lockTaken);

					if (_index < buffers.Length)
					{
						var buffer = buffers[_index];
						buffers[_index++] = null;

						if (buffer == null)
						{
							Debug.Fail("This should not happen, we tried to remove an empty bucket item");
						}
						else
						{
							_count--;
						}
					}
				}
				finally
				{
					if (lockTaken) _lock.Exit(false);
				}
			}

			/// <summary>
			/// Attempts to return the buffer to the bucket.  If successful, the buffer will be stored
			/// in the bucket and true will be returned; otherwise, the buffer won't be stored, and false
			/// will be returned.
			/// </summary>
			internal void Return(T[] array)
			{
				// Check to see if the buffer is the correct size for this bucket
				if (array.Length != _bufferLength)
				{
					throw new ArgumentException("Buffer is not from the pool", nameof(array));
				}

				// While holding the spin lock, if there's room available in the bucket,
				// put the buffer into the next available slot.  Otherwise, we just drop it.
				// The try/finally is necessary to properly handle thread aborts on platforms
				// which have them.
				bool lockTaken = false;
				try
				{
					_lock.Enter(ref lockTaken);

					if (_count == 0)
					{
						// Trim will see this as 0 and initialize it to the current time when Trim is called.
						_timestamp = TimeSpan.Zero;
					}

					if (_index != 0)
					{
						_buffers[--_index] = array;
						_count++;
					}
				}
				finally
				{
					if (lockTaken) _lock.Exit(false);
				}
			}

			static readonly TimeSpan StackTrimAfter = TimeSpan.FromSeconds(60);         // Trim after 60 seconds for low/moderate pressure
			static readonly TimeSpan StackHighTrimAfter = TimeSpan.FromSeconds(10);     // Trim after 10 seconds for high pressure
			const int StackLowTrimCount = 1;                                            // Trim one item when pressure is low
			const int StackMediumTrimCount = 2;                                         // Trim two items when pressure is moderate
			const int StackHighTrimCount = 8;                                           // Trim all items when pressure is high
			const int StackLargeBucket = 16384;                                         // If the bucket is larger than this we'll trim an extra when under high pressure
			const int StackModerateTypeSize = 16;                                       // If T is larger than this we'll trim an extra when under high pressure
			const int StackLargeTypeSize = 32;                                          // If T is larger than this we'll trim an extra (additional) when under high pressure

			internal void Trim(TimeSpan currentTime, AppMemoryUsageLevel usageLevel)
			{
				if (_count == 0)
				{
					return;
				}

				var trimDelay = usageLevel == AppMemoryUsageLevel.High ? StackHighTrimAfter : StackTrimAfter;

				lock (this)
				{
					if (_count == 0)
					{
						return;
					}

					if (_timestamp == TimeSpan.Zero)
					{
						_timestamp = currentTime;
						return;
					}

					if ((currentTime - _timestamp) <= trimDelay)
					{
						return;
					}

					var trimCount = StackLowTrimCount;
					switch (usageLevel)
					{
						case AppMemoryUsageLevel.High:
							trimCount = StackHighTrimCount;

							// When pressure is high, aggressively trim larger arrays.
							if (_bufferLength > StackLargeBucket)
							{
								trimCount++;
							}
							if (SizeOfT > StackModerateTypeSize)
							{
								trimCount++;
							}
							if (SizeOfT > StackLargeTypeSize)
							{
								trimCount++;
							}
							break;

						case AppMemoryUsageLevel.Medium:
							trimCount = StackMediumTrimCount;
							break;
					}

#if TRACE_REUSE
					Console.WriteLine($"ArrayPool<{typeof(T)}>(bucket:{_buffers.Length} sizeof:{Unsafe.SizeOf<T>()} created:{_created} reused:{_reused}) = {_count}");
#endif

					while (_count > 0 && trimCount-- > 0)
					{
						TrimOne();
					}

					_timestamp = _count > 0 ?
						_timestamp + TimeSpan.FromMilliseconds((trimDelay.TotalMilliseconds / 4)) : // Give the remaining items a bit more time
						TimeSpan.Zero;
				}
			}
		}
	}
}
