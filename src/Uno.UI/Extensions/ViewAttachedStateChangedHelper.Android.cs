#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Uno.UI
{
	/// <summary>
	/// A helper class supporting the management of View attached/detached from view listener.
	/// </summary>
	internal class ViewAttachedStateChangedHelper
	{
		private static Stack<AttachedChangedListener> _listenerPool = new Stack<AttachedChangedListener>();

		public static int MaxPoolInstances { get; set; }

		static ViewAttachedStateChangedHelper()
		{
			// An arbitrary limit for this pool, because it cannot grow unbounded...
			MaxPoolInstances = 100;
		}

		/// <summary>
		/// Gets an AttachedChangedListener using the provided actions.
		/// </summary>
		/// <param name="attachedHandler">An action called when the view is attached</param>
		/// <param name="detachedHandler">An action called when the view is detached</param>
		/// <returns>An AttachedChangedListener instance</returns>
		internal static AttachedChangedListener CreateChangedListener(Action<View> attachedHandler, Action<View> detachedHandler)
		{
			AttachedChangedListener listener;

			if (attachedHandler == null)
			{
				throw new ArgumentNullException(nameof(attachedHandler));
			}
			if (detachedHandler == null)
			{
				throw new ArgumentNullException(nameof(detachedHandler));
			}

			if (_listenerPool.Count != 0)
			{
				listener = _listenerPool.Pop();
			}
			else
			{
				listener = new AttachedChangedListener();
			}

			listener.AttachedHandler = attachedHandler;
			listener.DetachedHandler = detachedHandler;

			return listener;
		}

		/// <summary>
		/// Releases an AttachedChangedListener instance.
		/// </summary>
		/// <param name="listener">An AttachedChangedListener instance.</param>
		internal static void ReleaseChangedListener(AttachedChangedListener listener)
		{
			if (_listenerPool.Count < MaxPoolInstances)
			{
				listener.AttachedHandler = null;
				listener.DetachedHandler = null;

				_listenerPool.Push(listener);
			}
			else
			{
				listener.AttachedHandler = null;
				listener.DetachedHandler = null;

				listener.Dispose();
			}
		}

		internal class AttachedChangedListener : Java.Lang.Object, View.IOnAttachStateChangeListener
		{
			public Action<View> AttachedHandler { get; set; }
			public Action<View> DetachedHandler { get; set; }

			public AttachedChangedListener()
			{
			}

			public void OnViewAttachedToWindow(View attachedView)
			{
				if (AttachedHandler != null)
				{
					AttachedHandler(attachedView);
				}
			}

			public void OnViewDetachedFromWindow(View detachedView)
			{
				if (DetachedHandler != null)
				{
					DetachedHandler(detachedView);
				}
			}
		}
	}
}