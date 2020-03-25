using System;
using System.ComponentModel;
using Windows.UI.Xaml.Markup;

#if XAMARIN_IOS_UNIFIED
using UIKit;
#elif __MACOS__
using AppKit;
using UIView = AppKit.NSView;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class UserControl : ContentControl
	{

		// This property is present for binary compatibility with 
		// previous 
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new UIView Content
		{
			get { return (UIView)base.Content; }
			set { base.Content = value; }
		}
	}
}

