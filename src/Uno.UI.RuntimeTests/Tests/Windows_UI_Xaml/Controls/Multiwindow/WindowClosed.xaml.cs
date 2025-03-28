using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public sealed partial class WindowClosed : Window
{
	public WindowClosed()
	{
		this.InitializeComponent();
	}

	internal bool ClosedExecuted { get; private set; }

	private void Window_Closed(object sender, WindowEventArgs args)
	{
		ClosedExecuted = true;
	}
}
