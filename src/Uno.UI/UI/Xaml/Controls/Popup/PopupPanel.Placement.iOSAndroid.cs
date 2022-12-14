#if __IOS__ || __ANDROID__
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI;
using Windows.UI.Core;
using Uno.Extensions;

#if __IOS__
using View = UIKit.UIView;
#else
using Android.Views;
#endif

#nullable enable

namespace Windows.UI.Xaml.Controls.Primitives;

partial class PopupPanel
{
	/// <summary>
	/// A native view to use as the anchor, in the case that the managed <see cref="AnchorControl"/> is a proxy that's not actually
	/// included in the visual tree.
	/// </summary>
	protected virtual View? NativeAnchor => null;	
}

#endif
