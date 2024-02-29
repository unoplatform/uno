#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Uno.UI
{
	public static class ContextHelper
	{
		private static Android.Content.Context? _current;

		/// <summary>
		/// Get the current android content context
		/// </summary>
		public static Android.Content.Context Current
		{
			get
			{
				if (_current == null)
				{
					typeof(ContextHelper)
						.Log()
						.Warn(
							"ContextHelper.Current not defined. " +
							"For compatibility with Uno, you should ensure your `MainActivity` " +
							"is deriving from Windows.UI.Xaml.ApplicationActivity.");
				}
				return _current!;
			}
			set => _current = value;
		}

		/// <summary>
		/// Tries getting the current context.
		/// </summary>
		/// <param name="context">The context if available</param>
		/// <returns>true if the current context is available, otherwise false.</returns>
		internal static bool TryGetCurrent([NotNullWhen(true)] out Android.Content.Context? context)
		{
			context = _current;
			return _current != null;
		}
	}
}
#endif
