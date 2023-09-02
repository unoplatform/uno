using System;
using System.Collections.Generic;
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

namespace Uno.UI.RuntimeTests;

public sealed partial class When_ThemeResource_Style_Switch_Page : UserControl
{
	public When_ThemeResource_Style_Switch_Page()
	{
		this.InitializeComponent();
	}

	public Button TestButton => TestBtn;
}
