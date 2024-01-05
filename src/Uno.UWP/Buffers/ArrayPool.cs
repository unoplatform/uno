// Imported from https://github.com/dotnet/corefx/commit/d9d1e815ad6c642cf5d61afa4a16726548598bb2 until Xamarin exposes it properly.
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace Uno.Buffers
{
	/// <summary>
	/// Provides a resource pool that enables reusing instances of type <see cref="T:T[]"/>. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// Renting and returning buffers with an <see cref="ArrayPool{T}"/> can increase performance
	/// in situations where arrays are created and destroyed frequently, resulting in significant
	/// memory pressure on the garbage collector.
	/// </para>
	/// <para>
	/// This class is thread-safe.  All members may be used by multiple threads concurrently.
	/// </para>
	/// </remarks>
	internal partial class ArrayPool<T>
	{
		/// <summary>The lazily-initialized shared pool instance.</summary>
		private static ArrayPool<T>? s_sharedInstance;

		/// <summary>
		/// Retrieves a shared <see cref="ArrayPool{T}"/> instance.
		/// </summary>
		/// <remarks>
		/// The shared pool provides a default implementation of <see cref="ArrayPool{T}"/>
		/// that's intended for general applicability.  It maintains arrays of multiple sizes, and 
		/// may hand back a larger array than was actually requested, but will never hand back a smaller 
		/// array than was requested. Renting a buffer from it with <see cref="Rent"/> will result in an 
		/// existing buffer being taken from the pool if an appropriate buffer is available or in a new 
		/// buffer being allocated if one is not available.
		/// </remarks>
		public static ArrayPool<T> Shared
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return Volatile.Read(ref s_sharedInstance) ?? EnsureSharedCreated(); }
		}

		/// <summary>Ensures that <see cref="s_sharedInstance"/> has been initialized to a pool and returns it.</summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static ArrayPool<T> EnsureSharedCreated()
		{
			Interlocked.CompareExchange(ref s_sharedInstance, new ArrayPool<T>(automaticManagement: true), null);
			return s_sharedInstance;
		}

		/// <summary>
		/// Creates a new <see cref="ArrayPool{T}"/> instance using default configuration options.
		/// </summary>
		/// <returns>A new <see cref="ArrayPool{T}"/> instance.</returns>
		public static ArrayPool<T> Create()
		{
			return new ArrayPool<T>();
		}

		/// <summary>
		/// Creates a new <see cref="ArrayPool{T}"/> instance using custom configuration options.
		/// </summary>
		/// <param name="maxArrayLength">The maximum length of array instances that may be stored in the pool.</param>
		/// <param name="maxArraysPerBucket">
		/// The maximum number of array instances that may be stored in each bucket in the pool.  The pool
		/// groups arrays of similar lengths into buckets for faster access.
		/// </param>
		/// <returns>A new <see cref="ArrayPool{T}"/> instance with the specified configuration options.</returns>
		/// <remarks>
		/// The created pool will group arrays into buckets, with no more than <paramref name="maxArraysPerBucket"/>
		/// in each bucket and with those arrays not exceeding <paramref name="maxArrayLength"/> in length.
		/// </remarks>
		public static ArrayPool<T> Create(int maxArrayLength, int maxArraysPerBucket)
		{
			return new ArrayPool<T>(maxArrayLength, maxArraysPerBucket);
		}
	}
}
