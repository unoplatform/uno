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
	public sealed partial class CanvasZIndex : UserControl
	{
		public CanvasZIndex()
		{
			this.InitializeComponent();
		}
		public Border Get_CanvasBorderRed1()
		{
			return CanvasBorderRed1;
		}
		public Border Get_CanvasBorderRed2()
		{
			return CanvasBorderRed2;
		}
		public Border Get_CanvasBorderRed3()
		{
			return CanvasBorderRed3;
		}

		public Border Get_CanvasBorderGreen1()
		{
			return CanvasBorderGreen1;
		}
		public Border Get_CanvasBorderGreen2()
		{
			return CanvasBorderGreen1;
		}
		public Border Get_CanvasBorderGreen3()
		{
			return CanvasBorderGreen3;
		}
	}
}
