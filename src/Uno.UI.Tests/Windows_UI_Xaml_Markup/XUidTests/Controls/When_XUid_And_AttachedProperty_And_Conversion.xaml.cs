using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XUidTests.Controls
{
	public sealed partial class When_XUid_And_AttachedProperty_And_Conversion : UserControl
	{
		public When_XUid_And_AttachedProperty_And_Conversion()
		{
			this.InitializeComponent();
		}
	}

	public static class When_XUid_And_AttachedProperty_And_Conversion_KeyboardShortcutManager
	{
		public static VirtualKey GetVirtualKey(DependencyObject obj)
			=> (VirtualKey)obj.GetValue(VirtualKeyProperty);

		public static void SetVirtualKey(DependencyObject obj, VirtualKey value)
			=> obj.SetValue(VirtualKeyProperty, value);

		public static readonly DependencyProperty VirtualKeyProperty =
			DependencyProperty.RegisterAttached(
				nameof(VirtualKey),
				typeof(VirtualKey),
				typeof(When_XUid_And_AttachedProperty_And_Conversion_KeyboardShortcutManager),
				new PropertyMetadata(VirtualKey.None)
			);
	}
}
