using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.RuntimeTests;

public sealed partial class When_ThemeResource_Style_Switch_Page : UserControl
{
	public When_ThemeResource_Style_Switch_Page()
	{
		this.InitializeComponent();
	}

	public Button TestButton => TestBtn;
}
