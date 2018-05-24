using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Linq;
using Uno.Extensions;
using Windows.UI.Xaml.Data;

#if XAMARIN_ANDROID
using Android.Views;
#elif XAMARIN_IOS
using View = UIKit.UIView;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class RelativePanel : Panel
	{
		public RelativePanel() : base()
		{

#if XAMARIN_IOS
			this.ClipsToBounds = true;
#endif
		}
	}
}