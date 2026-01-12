using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
#if __ANDROID__
using INativeObject = Android.Runtime.IJavaObject;
#elif __APPLE_UIKIT__
using INativeObject = ObjCRuntime.INativeObject;
#endif

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// Defines a WeakReference with an owner, created through <see cref="WeakReferencePool.RentWeakReference(object, object)"/>
	/// </summary>
	public class ManagedWeakReference : IDisposable
	{
		private readonly ManagedGCHandle _targetHandle;
		private readonly ManagedGCHandle _ownerHandle;
		private bool _disposed;

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

				var target = _targetHandle?.Target;
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

			return new WeakReference(_targetHandle?.Target);
		}

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
				var target = _targetHandle?.Target;
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

		/// <summary>
		/// Tries to retrieve the target of the weak reference and cast it to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to cast the target to.</typeparam>
		/// <param name="target">When this method returns, contains the target object cast to type T if the target is alive and the cast succeeds; otherwise, the default value for type T.</param>
		/// <returns>true if the target is alive and was successfully cast to type T; otherwise, false.</returns>
		public bool TryGetTarget<T>(out T target) where T : class
		{
			if (IsAlive && Target is T castTarget)
			{
				target = castTarget;
				return true;
			}

			target = default;
			return false;
		}

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

#if !__ANDROID__ && !__APPLE_UIKIT__
#if IS_UNIT_TESTS
	public
#else
	internal
#endif
// Dummy interface to compile on WASM and unit tests. (On WASM isn't implemented by anything, so IsNativeAlive will always return true)
interface INativeObject
	{
		IntPtr Handle { get; }
	}
#endif
}
