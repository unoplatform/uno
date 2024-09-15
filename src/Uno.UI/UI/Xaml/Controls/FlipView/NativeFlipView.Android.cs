using System;
using System.Collections.Generic;
using System.Text;
using Android.Views;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NativeFlipView : NativePagedView
	{
		public override void OnViewAdded(View child) => base.OnViewAdded(child);
	}
}
