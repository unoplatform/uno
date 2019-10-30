using System;
using System.Drawing;
using System.Linq;
using Uno.Extensions;
using Windows.UI.Xaml;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#endif

namespace Windows.UI.Xaml
{
	public partial interface IFrameworkElement : DependencyObject
	{
	}
}
