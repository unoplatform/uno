
#if XAMARIN_IOS_UNIFIED
using System;
using System.ComponentModel;
using UIKit;
using Windows.UI.Xaml.Markup;
#elif XAMARIN_IOS
using MonoTouch.UIKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class UserControl : ContentControl
	{
		public UserControl ()
		{
			Initialize ();
		}


		void Initialize ()
		{
		}

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

