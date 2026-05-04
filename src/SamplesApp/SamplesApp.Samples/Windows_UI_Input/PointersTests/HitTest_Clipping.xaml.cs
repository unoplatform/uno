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
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.PointersTests;

[Sample("Pointers")]
public sealed partial class HitTest_Clipping : Page
{
	public HitTest_Clipping()
	{
		this.InitializeComponent();
	}
	private void PrepareScroller(object sender, RoutedEventArgs e)
	{
#pragma warning disable CS0618 // Type or member is obsolete
		The_Scroller.ScrollToVerticalOffset(100);
#pragma warning restore CS0618 // Type or member is obsolete
	}

	private void OnTargetClicked(object sender, RoutedEventArgs e)
	{
		The_Output.Text = (sender as FrameworkElement)?.Name ?? sender?.GetType().Name ?? "--unknown--";
	}
}
