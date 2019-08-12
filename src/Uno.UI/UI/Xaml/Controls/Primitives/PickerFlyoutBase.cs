using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class PickerFlyoutBase : FlyoutBase
	{
#if __IOS__ || __ANDROID__
		protected PickerFlyoutBase()
		{
		}
#endif
	}
}
