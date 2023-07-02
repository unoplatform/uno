using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Uno.UI;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __IOS__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#endif

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A generic control layouter, to apply size, alignment and margins to a single child.
	/// </summary>
	partial class Control
	{
	}
}
