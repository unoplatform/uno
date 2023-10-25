using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System.Collections.Generic;
using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

#if !WINDOWS_UWP
using Microsoft.UI.Input;
#endif

namespace UITests.Windows_UI_Xaml_Input
{
	[SampleControlInfo("Windows.UI.Xaml.Input", "XamlUICommand", description: "Demonstrates use of XamlUICommand and StandardUICommand")]
	public sealed partial class XamlUICommandTests : Page
	{
		public XamlUICommandTests()
		{
			this.InitializeComponent();
		}
	}
}
