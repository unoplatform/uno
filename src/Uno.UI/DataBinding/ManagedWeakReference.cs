using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// Defines a WeakReference with an owner, created through <see cref="WeakReferencePool.RentWeakReference(object, object)"/>
	/// </summary>
	public class ManagedWeakReference : IDisposable
	{
		private readonly ManagedGCHandle _targetHandle;
		private readonly ManagedGCHandle _ownerHandle;
		private volatile bool _disposed;

		internal ManagedWeakReference(ManagedGCHandle ownerHandle, ManagedGCHandle targetHandle, bool isSelf)
		{
			_ownerHandle = ownerHandle;
			_targetHandle = targetHandle;
			IsSelfReference = isSelf;
		}

		/// <summary>
		/// Provides the owner of the reference, which requested the current <see cref="WeakReference"/>
		/// </summary>
		public object Owner => _ownerHandle?.Target;

		/// <summary>
		/// The managed weak reference
		/// </summary>
		public object Target
		{
			get
			{
				if (_disposed)
				{
					throw new ObjectDisposedException(nameof(ManagedWeakReference));
				}

				var target = ReadTarget();

				if (IsNativeAlive(target))
				{
					return target;
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Provides the inner <see cref="WeakReference"/> to the target
		/// </summary>
		internal ManagedGCHandle GetUnsafeTargetHandle()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(ManagedWeakReference));
			}

			return _targetHandle;
		}


		/// <summary>
		/// Provides the inner <see cref="WeakReference"/> to the target
		/// </summary>
		public WeakReference CloneWeakReference()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(ManagedWeakReference));
			}

			return new WeakReference(ReadTarget());
		}

		/// <summary>
		/// The target, or null if it was collected or this reference was returned to the pool. Unlike <see cref="Target"/>,
		/// never throws: for readers that may race a return, such as an INotifyPropertyChanged raised off the UI thread.
		/// </summary>
		internal object TargetOrNull
		{
			get
			{
				var target = ReadTarget();
				return IsNativeAlive(target) ? target : null;
			}
		}

		// Null if disposed before or during the read: the pool may have recycled the handle. The barrier stops ARM64
		// from completing the target read after the flag read (an off-thread INotifyPropertyChanged reader).
		// Only load-load ordering is needed: Volatile.ReadBarrier is free on x64 and a "dmb ishld" on ARM64, where
		// a full fence costs about 10 ns per read on x64. This runs on every Target, IsAlive and TryGetTarget.
		private object ReadTarget()
		{
			var target = _targetHandle?.Target;

#if NET10_0_OR_GREATER
			if (_runsOnMono)
			{
				Interlocked.MemoryBarrier();
			}
			else
			{
				Volatile.ReadBarrier();
			}
#else
			Interlocked.MemoryBarrier();
#endif

			return _disposed ? null : target;
		}

#if NET10_0_OR_GREATER
		// Volatile.ReadBarrier is a JIT intrinsic whose managed body calls itself. CoreCLR replaces it; the Mono
		// interpreter (WebAssembly, and the interpreted code of the mobile heads) runs the body and overflows the stack
		// on the first weak-reference read. These are the platforms where .NET runs on Mono, so they take the full fence
		// instead (what .NET 9 uses). An Android head on CoreCLR also takes it, which is correct, only slower.
		private static readonly bool _runsOnMono =
			OperatingSystem.IsBrowser()
			|| OperatingSystem.IsWasi()
			|| OperatingSystem.IsAndroid()
			|| OperatingSystem.IsIOS()
			|| OperatingSystem.IsTvOS()
			|| OperatingSystem.IsMacCatalyst();
#endif

		/// <summary>
		/// Provides the inner <see cref="WeakReference"/> to the owner
		/// </summary>
		internal ManagedGCHandle GetUnsafeOwnerHandle()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(ManagedWeakReference));
			}

			return _ownerHandle;
		}

		/// <summary>
		/// <see cref="WeakReference.IsAlive"/>
		/// </summary>
		public bool IsAlive
		{
			get
			{
				if (_disposed)
				{
					return false;
				}
				var target = ReadTarget();
				if (!IsNativeAlive(target))
				{
					return false;
				}
				return target != null;
			}
		}

		/// <summary>
		/// Determines if this instance is a self reference
		/// </summary>
		public bool IsSelfReference { get; }

#nullable enable
		/// <summary>
		/// Tries to retrieve the target of the weak reference and cast it to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to cast the target to.</typeparam>
		/// <param name="target">When this method returns, contains the target object cast to type T if the target is alive and the cast succeeds; otherwise, the default value for type T.</param>
		/// <returns>true if the target is alive and was successfully cast to type T; otherwise, false.</returns>
		public bool TryGetTarget<T>([NotNullWhen(true)] out T? target) where T : class
		{
			target = default;

			// Return false if disposed, without throwing
			if (_disposed)
			{
				return false;
			}

			// Get the target once and check if it's alive
			var rawTarget = ReadTarget();
			if (rawTarget == null || !IsNativeAlive(rawTarget))
			{
				return false;
			}

			// Try to cast to the requested type
			if (rawTarget is T castTarget)
			{
				target = castTarget;
				return true;
			}

			return false;
		}
#nullable restore

		/// <summary>
		/// Check if the target is a managed peer whose underlying native object has been collected.
		/// </summary>
		private static bool IsNativeAlive(object obj)
		{
			if (obj is INativeObject nativeObj)
			{
				return nativeObj.Handle != IntPtr.Zero;
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// Determines if the current instance has been disposed via <see cref="Dispose"/>.
		/// </summary>
		public bool IsDisposed => _disposed;

		/// <summary>
		/// Disposes the current instance
		/// </summary>
		public void Dispose()
		{
			_disposed = true;
		}

	}


	// Dummy interface to compile on WASM and unit tests. (On WASM isn't implemented by anything, so IsNativeAlive will always return true)
	internal interface INativeObject
	{
		IntPtr Handle { get; }
	}
}
