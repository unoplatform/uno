using System.Linq;
using Uno.Extensions;
using Uno.UI;
using Windows.UI.Xaml.Controls;
using System;
#if XAMARIN_IOS
using Foundation;
using UIKit;
#elif XAMARIN_ANDROID
using Android.Text.Style;
#endif

namespace Windows.UI.Xaml.Documents
{
	internal static class RunExtensions
    {
		/// <summary>
		/// Sets the style for the current run.
		/// </summary>
		public static Run Style(this Run run, Style style)
		{
			run.Style = style;
			return run;
		}
	}
}