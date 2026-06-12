using System;
using System.Collections.Generic;
using System.Text;
#if __APPLE_UIKIT__
using _NativeBase = UIKit.UISwitch;
#elif __ANDROID__
using _NativeBase = AndroidX.AppCompat.Widget.AppCompatCheckBox;
#else
using _NativeBase = Microsoft.UI.Xaml.Controls.CheckBox; // No native views on other platforms
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class NativeView : _NativeBase
	{
#if __ANDROID__
		public NativeView() : base(ContextHelper.Current)
		{

		}
#endif
	}
}
