using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Uno.UI;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// A generic control layouter, to apply size, alignment and margins to a single child.
	/// </summary>
	partial class Control
	{
	}
}
