using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// A <see cref="WeakReference"/>-like implementation suited for reuse in <see cref="WeakReferencePool"/>
	/// </summary>
	/// <remarks>
	/// This class is very similar to <see cref="WeakReference"/> except that it does
	/// not create an empty GCHandle when created. It also works around an unknown <see cref="WeakReference"/> issue
	/// where setting the <see cref="WeakReference.Target"/> with a live object always returns null, yet creating a 
	/// <see cref="WeakReference"/> using the same live object creates a normal weak reference.
	/// </remarks>
	internal class ManagedGCHandle : IDisposable
	{
		private GCHandle _handle;

		private bool _isDisposed;

		/// <summary>
		/// Provides the target of the GCHandle
		/// </summary>
		public object Target
		{
			get => _handle.IsAllocated ? _handle.Target : null;
			set
			{
				if(!_handle.IsAllocated)
				{
					_handle = GCHandle.Alloc(value, GCHandleType.Weak);
				}
				else
				{
					_handle.Target = value;
				}
			}
		}

		internal GCHandle Handle => _handle;

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}

			if(_handle.IsAllocated)
			{
				_handle.Free();
			}

			_isDisposed = true;
		}

		~ManagedGCHandle()
		{
			this.Dispose(false);
		}
	}
}
