using Microsoft/* UWP don't rename */.UI.Xaml;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System.Collections.Generic;
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

#if !WINAPPSDK
using Microsoft.UI.Input;
#endif

namespace UITests.Windows_UI_Xaml_Input;

[Sample("Microsoft.UI.Xaml.Input", Name = "XamlUICommand", Description = "You should see Cut button without icon, Copy button with icon, Home button without icon and Forward we go! with icon.", IsManualTest = true)]
public sealed partial class XamlUICommandTests : Page
{
	public XamlUICommandTests()
	{
		this.InitializeComponent();
	}
}
