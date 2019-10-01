using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Uno.Extensions;

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
		public delegate void EventRaiseHandler(Delegate @delegate, object source, object args);

		/// <summary>
		/// An abstract handler to be called when raising the weak event.
		/// </summary>
		/// <param name="source">The source of the event</param>
		/// <param name="args">
		/// The args of the event, which must match the explicit cast
		/// performed in the matching <see cref="EventRaiseHandler"/> instance.
		/// </param>
		public delegate void GenericEventHandler(object source, object args);

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
		/// If the returned disposable is collected, the handler will also be
		/// collected. Conversely, if the <paramref name="list"/> is collected
		/// raising the event will produce nothing.
		/// </remarks>
		internal static IDisposable RegisterEvent(IList<GenericEventHandler> list, Delegate handler, EventRaiseHandler raise)
		{
			var wr = new WeakReference(handler);

			GenericEventHandler genericHandler = null;

			// This weak reference ensure that the closure will not link
			// the caller and the callee, in the same way "newValueActionWeak" 
			// does not link the callee to the caller.
			var instanceRef = new WeakReference<IList<GenericEventHandler>>(list);

			Action removeHandler = () => 
			{
				var thatList = instanceRef.GetTarget();

				if (thatList != null)
				{
					thatList.Remove(genericHandler);
				}
			};

			genericHandler = (s, e) =>
			{
				var weakHandler = wr.Target as Delegate;

				if (weakHandler != null)
				{
					raise(weakHandler, s, e);
				}
				else
				{
					removeHandler();
				}
			};

			list.Add(genericHandler);

			return Disposable.Create(() =>
			{
				removeHandler();

				// Force a closure on the callback, to make its lifetime as long 
				// as the subscription being held by the callee.
				handler = null;
			});
		}

	}
}
