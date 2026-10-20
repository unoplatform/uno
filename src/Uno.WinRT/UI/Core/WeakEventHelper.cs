#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Buffers;
using Uno.Disposables;
using Uno.Extensions;
using Windows.Security.Cryptography.Core;

namespace Windows.UI.Core
{
	internal class WeakEventHelper
	{
		/// <summary>
		/// Defines a event raise method.
		/// </summary>
		/// <param name="delegate">The delegate to call with <paramref name="source"/> and <paramref name="args"/>.</param>
		/// <param name="source">The source of the event raise</param>
		/// <param name="args">The args used to raise the event</param>
		public delegate void EventRaiseHandler(Delegate @delegate, object source, object? args);

		/// <summary>
		/// An abstract handler to be called when raising the weak event.
		/// </summary>
		/// <param name="source">The source of the event</param>
		/// <param name="args">
		/// The args of the event, which must match the explicit cast
		/// performed in the matching <see cref="EventRaiseHandler"/> instance.
		/// </param>
		public delegate void GenericEventHandler(object source, object? args);

		/// <summary>
		/// Provides an abstraction for GC trimming registration
		/// </summary>
		internal interface ITrimProvider
		{
			/// <summary>
			/// Registers a callback to be called when GC triggered
			/// </summary>
			/// <param name="callback">Function called with <paramref name="target"/> as the first parameter</param>
			/// <param name="target">instance to be provided when invoking <paramref name="callback"/></param>
			void RegisterTrimCallback(Func<object, bool> callback, object target);
		}

		/// <summary>
		/// Default trimming implementation which uses the actual GC implementation
		/// </summary>
		private class DefaultTrimProvider : ITrimProvider
		{
			public void RegisterTrimCallback(Func<object, bool> callback, object target)
			{
				Windows.Foundation.Gen2GcCallback.Register(callback, target);
			}
		}

		/// <summary>
		/// A collection of weak event registrations
		/// </summary>
		public class WeakEventCollection : IDisposable
		{
			private record WeakHandler(WeakReference Target, GenericEventHandler Handler);

			private object _lock = new object();
			private List<WeakHandler> _handlers = [];
			private readonly ITrimProvider _provider;

			// Explicit parameterless constructor to allow Activator.CreateInstance to
			// dynamically create an instance of WeakEventCollection via reflection.
			public WeakEventCollection() : this(null)
			{
			}

			public WeakEventCollection(ITrimProvider? provider = null)
			{
				_provider = provider ?? new DefaultTrimProvider();
			}

			public bool IsEmpty
			{
				get
				{
					lock (_lock)
					{
						return _handlers.Count == 0;
					}
				}
			}

			private bool Trim()
			{
				lock (_lock)
				{
					for (int i = _handlers.Count - 1; i >= 0; i--)
					{
						if (!_handlers[i].Target.IsAlive)
						{
							_handlers.RemoveAt(i);
						}
					}

					return _handlers.Count != 0;
				}
			}

			/// <summary>
			/// Invokes all the alive registered handlers
			/// </summary>
			public void Invoke(object sender, object? args)
			{
				lock (_lock)
				{
					if (_handlers.Count == 1)
					{
						// Fast path for single registrations
						var handler = _handlers[0];
						handler.Handler(sender, args);
					}
					else if (_handlers.Count > 1)
					{
						// Clone the array to account for reentrancy.
						var handlers = ArrayPool<WeakHandler>.Shared.Rent(_handlers.Count);
						_handlers.CopyTo(handlers, 0);

						// Use the original count, the rented array may be larger.
						var count = _handlers.Count;

						for (int i = 0; i < count; i++)
						{
							ref var handler = ref handlers[i];

							handler.Handler(sender, args);

							// Clear the handle immediately, so we don't
							// call ArrayPool.Return with clear.
							handler = null;
						}

						ArrayPool<WeakHandler>.Shared.Return(handlers);
					}
				}
			}

			/// <summary>
			/// Do not use directly, use <see cref="RegisterEvent"/> instead.
			/// Registers an handler to be called when the event is raised. 
			/// </summary>
			/// <returns>A disposable that can be used to unregister the provided handler.</returns>
			internal IDisposable Register(WeakReference target, GenericEventHandler handler, Delegate actualTarget)
			{
				lock (_lock)
				{
					WeakHandler key = new(target, handler);
					_handlers.Add(key);

					if (_handlers.Count == 1)
					{
						_provider.RegisterTrimCallback(_ => Trim(), this);
					}

					return Disposable.Create(RemoveRegistration);

					void RemoveRegistration()
					{
						// Force a capture of the delegate by setting it to null.
						// This will ensure that keeping the IDisposable alive will
						// keep the registered original delegate alive. This does ensure
						// that if the original delegate was provided as a lambda, the
						// instance will stay active as long as the disposable instance.
						actualTarget = null!;

						lock (_lock)
						{
							_handlers.Remove(key);
						}
					}
				}
			}

			public void Dispose()
			{
				lock (_lock)
				{
					_handlers.Clear();
				}
			}
		}

		/// <summary>
		/// Provides a bi-directional weak event handler management.
		/// </summary>
		/// <param name="list">A list of registrations to manage</param>
		/// <param name="handler">The actual handler to execute.</param>
		/// <param name="raise">The delegate used to raise <paramref name="handler"/> if it has not been collected.</param>
		/// <returns>A disposable that keeps the registration alive.</returns>
		/// <remarks>
		/// The bi-directional relation is defined by the fact that both the 
		/// source and the target are weak. The source must be kept alive by 
		/// another longer-lived reference, and the target is kept alive by the
		/// return disposable.
		/// 
		/// If either the <paramref name="list"/> or the <paramref name="handler"/> are 
		/// collected, raising the event will produce nothing.
		/// </remarks>
		internal static IDisposable RegisterEvent(WeakEventCollection list, Delegate handler, EventRaiseHandler raise)
		{
			var wr = new WeakReference(handler);

			GenericEventHandler? genericHandler = null;

			// This weak reference ensure that the closure will not link
			// the caller and the callee, in the same way "newValueActionWeak" 
			// does not link the callee to the caller.
			var instanceRef = new WeakReference<WeakEventCollection>(list);

			genericHandler = (s, e) =>
			{
				var weakHandler = wr.Target as Delegate;

				if (weakHandler != null)
				{
#if __ANDROID__
					// If the target is a IJavaPeerable that does not have a valid peer reference, there
					// is no need to call the handler. If it's not a IJavaPeerable, call the target.
					// This scenario may happen when the object is about to be collected and has already
					// collected its native counterpart. We know that the target will likely try to use
					// native methods, which will fail if the peer reference is not valid.
					var javaPeerable = weakHandler.Target as Java.Interop.IJavaPeerable;
					if (javaPeerable?.PeerReference.IsValid ?? true)
#endif
					{

						raise(weakHandler, s, e);
					}
				}
			};

			return list.Register(wr, genericHandler, handler);
		}
	}
}
