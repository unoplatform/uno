using Android.Widget;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.Controls
{
	public partial class BindableProgressBar : ProgressBar, DependencyObject
	{
		public BindableProgressBar() 
			: base(ContextHelper.Current)
		{
			InitializeBinder();
		}
	}
}
