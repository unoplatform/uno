// Imported from https://github.com/dotnet/corefx/commit/d9d1e815ad6c642cf5d61afa4a16726548598bb2 until Xamarin exposes it properly.
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Uno.Buffers
{
    internal sealed partial class ArrayPool<T>
    {
        /// <summary>The default maximum length of each array in the pool (2^20).</summary>
        private const int DefaultMaxArrayLength = 1024 * 1024;
        /// <summary>The default maximum number of arrays per bucket that are available for rent.</summary>
        private const int DefaultMaxNumberOfArraysPerBucket = 50;
        /// <summary>Lazily-allocated empty array used when arrays of length 0 are requested.</summary>
        private static T[] s_emptyArray; // we support contracts earlier than those with Array.Empty<T>()

        private readonly Bucket[] _buckets;

        internal ArrayPool() : this(DefaultMaxArrayLength, DefaultMaxNumberOfArraysPerBucket)
        {
        }

        internal ArrayPool(int maxArrayLength, int maxArraysPerBucket)
        {
            if (maxArrayLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxArrayLength));
            }
            if (maxArraysPerBucket <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxArraysPerBucket));
            }

            // Our bucketing algorithm has a min length of 2^4 and a max length of 2^30.
            // Constrain the actual max used to those values.
            const int MinimumArrayLength = 0x10, MaximumArrayLength = 0x40000000;
            if (maxArrayLength > MaximumArrayLength)
            {
                maxArrayLength = MaximumArrayLength;
            }
            else if (maxArrayLength < MinimumArrayLength)
            {
                maxArrayLength = MinimumArrayLength;
            }

            // Create the buckets.
            int poolId = Id;
            int maxBuckets = Utilities.SelectBucketIndex(maxArrayLength);
            var buckets = new Bucket[maxBuckets + 1];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new Bucket(Utilities.GetMaxSizeForBucket(i), maxArraysPerBucket, poolId);
            }
            _buckets = buckets;
        }

        /// <summary>Gets an ID for the pool to use with events.</summary>
        private int Id => GetHashCode();

		/// <summary>
		/// Retrieves a buffer that is at least the requested length.
		/// </summary>
		/// <param name="minimumLength">The minimum length of the array needed.</param>
		/// <returns>
		/// An <see cref="T:T[]"/> that is at least <paramref name="minimumLength"/> in length.
		/// </returns>
		/// <remarks>
		/// This buffer is loaned to the caller and should be returned to the same pool via 
		/// <see cref="Return"/> so that it may be reused in subsequent usage of <see cref="Rent"/>.  
		/// It is not a fatal error to not return a rented buffer, but failure to do so may lead to 
		/// decreased application performance, as the pool may need to create a new buffer to replace
		/// the one lost.
		/// </remarks>
		public T[] Rent(int minimumLength)
        {
            // Arrays can't be smaller than zero.  We allow requesting zero-length arrays (even though
            // pooling such an array isn't valuable) as it's a valid length array, and we want the pool
            // to be usable in general instead of using `new`, even for computed lengths.
            if (minimumLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumLength));
            }
            else if (minimumLength == 0)
            {
                // No need for events with the empty array.  Our pool is effectively infinite
                // and we'll never allocate for rents and never store for returns.
                return s_emptyArray ?? (s_emptyArray = new T[0]);
            }

            T[] buffer = null;

            int index = Utilities.SelectBucketIndex(minimumLength);
            if (index < _buckets.Length)
            {
                // Search for an array starting at the 'index' bucket. If the bucket is empty, bump up to the
                // next higher bucket and try that one, but only try at most a few buckets.
                const int MaxBucketsToTry = 2;
                int i = index;
                do
                {
                    // Attempt to rent from the bucket.  If we get a buffer from it, return it.
                    buffer = _buckets[i].Rent();
                    if (buffer != null)
                    {
                        return buffer;
                    }
                }
                while (++i < _buckets.Length && i < index + MaxBucketsToTry);

                // The pool was exhausted for this buffer size.  Allocate a new buffer with a size corresponding
                // to the appropriate bucket.
                buffer = new T[_buckets[index]._bufferLength];
            }
            else
            {
                // The request was for a size too large for the pool.  Allocate an array of exactly the requested length.
                // When it's returned to the pool, we'll simply throw it away.
                buffer = new T[minimumLength];
            }

            return buffer;
        }

		/// <summary>
		/// Returns to the pool an array that was previously obtained via <see cref="Rent"/> on the same 
		/// <see cref="ArrayPool{T}"/> instance.
		/// </summary>
		/// <param name="array">
		/// The buffer previously obtained from <see cref="Rent"/> to return to the pool.
		/// </param>
		/// <param name="clearArray">
		/// If <c>true</c> and if the pool will store the buffer to enable subsequent reuse, <see cref="Return"/>
		/// will clear <paramref name="array"/> of its contents so that a subsequent consumer via <see cref="Rent"/> 
		/// will not see the previous consumer's content.  If <c>false</c> or if the pool will release the buffer,
		/// the array's contents are left unchanged.
		/// </param>
		/// <remarks>
		/// Once a buffer has been returned to the pool, the caller gives up all ownership of the buffer 
		/// and must not use it. The reference returned from a given call to <see cref="Rent"/> must only be
		/// returned via <see cref="Return"/> once.  The default <see cref="ArrayPool{T}"/>
		/// may hold onto the returned buffer in order to rent it again, or it may release the returned buffer
		/// if it's determined that the pool already has enough buffers stored.
		/// </remarks>
		public void Return(T[] array, bool clearArray = false)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            else if (array.Length == 0)
            {
                // Ignore empty arrays.  When a zero-length array is rented, we return a singleton
                // rather than actually taking a buffer out of the lowest bucket.
                return;
            }

            // Determine with what bucket this array length is associated
            int bucket = Utilities.SelectBucketIndex(array.Length);

            // If we can tell that the buffer was allocated, drop it. Otherwise, check if we have space in the pool
            if (bucket < _buckets.Length)
            {
                // Clear the array if the user requests
                if (clearArray)
                {
                    Array.Clear(array, 0, array.Length);
                }

                // Return the buffer to its bucket.  In the future, we might consider having Return return false
                // instead of dropping a bucket, in which case we could try to return to a lower-sized bucket,
                // just as how in Rent we allow renting from a higher-sized bucket.
                _buckets[bucket].Return(array);
            }
        }
    }
}
