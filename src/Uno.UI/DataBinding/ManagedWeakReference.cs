using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Extensions;
using Uno.Logging;

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

				return _targetHandle?.Target;
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
		public bool IsAlive => !_disposed && _targetHandle?.Target != null;

		/// <summary>
		/// Determines if this instance is a self reference
		/// </summary>
		public bool IsSelfReference { get; }

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
}
