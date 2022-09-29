#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Uno.Extensions;

namespace Uno.UI
{
	internal static class ViewEventExtensions
	{
		public static View InitializeClick(this View view, bool isClickable, ICollection<IDisposable> subscriptions, Action<object, EventArgs> selector)
		{
			if (isClickable)
			{
				view.Clickable = isClickable;

				EventHandler handler = (s, e) => selector(s, e);

				view.Click += handler;

				subscriptions.Add(Disposable.Create(() => view.Click -= handler));
			}

			return view;
		}

		public static View InitializeLongClick(this View view, bool isLongClickable, ICollection<IDisposable> subscriptions, Action<object, View.LongClickEventArgs> selector)
		{
			if (isLongClickable)
			{
				view.LongClickable = isLongClickable;

				EventHandler<View.LongClickEventArgs> handler = (s, e) => selector(s, e);

				view.LongClick += handler;

				subscriptions.Add(Disposable.Create(() => view.LongClick -= handler));
			}

			return view;
		}

	}
}