using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Uno.UI;
using Windows.UI.ViewManagement;

namespace Uno.UI.ViewManagement
{
	public static class ApplicationViewHelper
	{
		/// <summary>
		/// Gets an instance to <see cref="IBaseActivityEvents"/> which provides a set of events
		/// raised on key Activity method overrides.
		/// </summary>
		public static IBaseActivityEvents GetBaseActivityEvents()
			=> global::Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().BaseActivityEvents;
	}
}
