using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public sealed partial class Canvas_With_Outer_Clip : UserControl
	{
		public Canvas_With_Outer_Clip()
		{
			this.InitializeComponent();
		}

		public Border Get_LocatorBorder1()
		{
			return LocatorBorder1;
		}
		public Border Get_LocatorBorder2()
		{
			return LocatorBorder2;
		}

	}
}
