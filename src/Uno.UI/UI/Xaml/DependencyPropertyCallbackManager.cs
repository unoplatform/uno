using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Uno.Buffers;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// DependencyProperty Callback Manager
	/// </summary>
	/// <remarks>
	/// This class is built are the fact that creating arrays is costly, 
	/// so the list of handlers is kept alive for as long as possible.
	/// </remarks>
	internal class DependencyPropertyCallbackManager : IDisposable
	{
		private readonly LinkedList<PropertyChangedCallback> _callbacks = new LinkedList<PropertyChangedCallback>();
		private readonly static ArrayPool<PropertyChangedCallback> _pool = ArrayPool<PropertyChangedCallback>.Shared;
		private PropertyChangedCallback[] _callbacksShadow;
		private int _id;
		private bool _disposed;

		public DependencyPropertyCallbackManager()
		{
		}

		public void Dispose()
		{
			if (_disposed)
			{
#if DEBUG
				// Dispose here may not be invoked multiple times. If this is the case,
				// this means that the Dispose method from DependencyObjectStore is invoked
				// multiple times from different threads and is not synchronized properly.
				Debug.Fail("Dispose may not be invoked multiple times.");
#endif
				return;
			}
			_disposed = true;
			ReturnShadowToPool();
		}

		/// <summary>
		/// Registers a callback to be called by <see cref="RaisePropertyChanged(DependencyObject, DependencyPropertyChangedEventArgs)"/>
		/// </summary>
		/// <param name="callback">A property changed callback</param>
		/// <returns>A disposable to remove the callback from the manager</returns>
		public IDisposable RegisterCallback(PropertyChangedCallback callback)
		{
			// When a new callback is added, clear the cache if it has been created.
			ReturnShadowToPool();

			// The list has changed, change its id so the RaisePropertyChanged method can detect it
			_id++;

			return new CallbackDisposable(this, _callbacks.AddLast(callback));
		}

		public void RaisePropertyChanged(DependencyObject actualInstanceAlias, DependencyPropertyChangedEventArgs eventArgs)
		{
			EnsureCallbacksShadow();

			// Capture the current shadow list, as it may get updated by handlers
			// through the Invoke below.
			var localShadow = _callbacksShadow;
			var localCount = _callbacks.Count;
			var localId = _id;

			// The shadow is being set to null so it does not get returned to the pool if the
			// current list of handlers changes.
			_callbacksShadow = null;

			for (int cbIndex = 0; cbIndex < localCount; cbIndex++)
			{
				localShadow[cbIndex].Invoke(actualInstanceAlias, eventArgs);
			}

			if (_id == localId && _callbacksShadow == null)
			{
				// The list has not changed, and not other shadow list has been created.
				_callbacksShadow = localShadow;
			}
			else
			{
				// Another list has been created, return our captured instance to the pool.
				_pool.Return(localShadow, clearArray: true);
			}
		}

		private void EnsureCallbacksShadow()
		{
			if (_callbacksShadow == null)
			{
				if (_callbacks.Count != 0)
				{
					_callbacksShadow = _pool.Rent(_callbacks.Count);
					_callbacks.CopyTo(_callbacksShadow, 0);

					// Register for the finalizer, so _callbacksShadow can be returned to the pool
					GC.ReRegisterForFinalize(this);
				}
				else
				{
					_callbacksShadow = Array.Empty<PropertyChangedCallback>();
				}
			}
		}

		private void ReturnShadowToPool()
		{
			if (_callbacksShadow != null)
			{
				// The returned array needs to be cleaned, as it may hold information that should not leak
				_pool.Return(_callbacksShadow, clearArray: true);
				_callbacksShadow = null;

				// No need for the finalizer, the array was already returned to the pool.
				GC.SuppressFinalize(this);
			}
		}

		private class CallbackDisposable : IDisposable
		{
			private readonly LinkedListNode<PropertyChangedCallback> _cookie;
			private readonly DependencyPropertyCallbackManager _owner;

			public CallbackDisposable(DependencyPropertyCallbackManager owner, LinkedListNode<PropertyChangedCallback> cookie)
			{
				_cookie = cookie;
				_owner = owner;
			}

			public void Dispose()
			{
				_owner._callbacks.Remove(_cookie);

				// The list has changed, change its id so the RaisePropertyChanged method can detect it
				_owner._id++;

				// The current shadow is not valid anymore, release it.
				_owner.ReturnShadowToPool();
			}
		}
	}
}
