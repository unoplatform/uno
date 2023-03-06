using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public sealed partial class xBind_PathLessCasting : UserControl
	{
		public xBind_PathLessCasting()
		{
			this.InitializeComponent();
		}

		private string MyFunction(string p) => p;
		private string MyFunction2(string p1, string p2) => $"{p1}-{p2}";

		public static explicit operator string(xBind_PathLessCasting p) => $"ExplicitConversion_{p}";
	}
}
