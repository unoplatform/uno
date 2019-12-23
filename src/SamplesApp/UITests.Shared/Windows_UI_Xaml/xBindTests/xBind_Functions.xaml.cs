using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Xaml.xBindTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[SampleControlInfo("XBind")]
	public sealed partial class xBind_Functions : Page
    {
        public xBind_Functions()
        {
            this.InitializeComponent();
        }

		private string Add(double a, double b)
			=> (a + b).ToString();
	}

	public static class StaticType
	{
		public static int PropertyIntValue { get; } = 42;

		public static int PropertyStringValue { get; } = 42;

		public static string Add(double a, double b)
			=> (a + b).ToString();
	}
}
