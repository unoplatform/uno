using System;
using Uno.UI;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;

using View = Windows.UI.Xaml.UIElement;

namespace Windows.UI.Xaml.Input
{
	public partial class FocusManager
	{
		private static View InnerGetFocusedElement()
		{
			throw new NotImplementedException();
		}

		private static bool InnerTryMoveFocus(FocusNavigationDirection focusNavigationDirection)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Enumerates focusable views ordered by "cousin level".
		/// "Sister" views will be returned first, then first cousins, then second cousins, and so on.
		/// </summary>
		private static IEnumerable<View> SearchOtherFocusableViews(View currentView)
		{
			throw new NotImplementedException();
		}


		private static bool IsFocusableView(View view)
		{
			throw new NotImplementedException();
		}

		private static int[] GetAbsolutePosition(View v)
		{
			throw new NotImplementedException();
		}

		public static View InnerFindNextFocusableElement(FocusNavigationDirection focusNavigationDirection)
		{
			throw new NotImplementedException();
		}

		public static DependencyObject InnerFindFirstFocusableElement(DependencyObject searchScope)
		{
			throw new NotImplementedException();
		}

		private static DependencyObject InnerFindLastFocusableElement(DependencyObject searchScope)
		{
			throw new NotImplementedException();
		}

		private static void FocusNative(object toFocus) { }
	}
}
