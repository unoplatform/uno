using System;
using System.Collections.Generic;
using System.Text;
#if __IOS__
using _NativeBase = UIKit.UISwitch;
#elif __ANDROID__
using _NativeBase = AndroidX.AppCompat.Widget.AppCompatCheckBox;
#elif __MACOS__
using _NativeBase = AppKit.NSSwitch;
#else
using _NativeBase = Windows.UI.Xaml.Controls.CheckBox; // No native views on other platforms
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
