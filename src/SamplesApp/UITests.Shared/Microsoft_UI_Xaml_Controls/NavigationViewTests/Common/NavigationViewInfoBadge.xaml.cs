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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MUXControlsTestApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("NavigationView", "MUX", IgnoreInSnapshotTests = true)]
	public sealed partial class NavigationViewInfoBadgeTestPage : MUXControlsTestApp.TestPage
	{
		public NavigationViewInfoBadgeTestPage()
		{
			this.InitializeComponent();
		}

		private void FlipOrientationButton_Clicked(object sender, RoutedEventArgs e)
		{
			NavView.PaneDisplayMode = NavView.PaneDisplayMode == Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Top ? Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Auto : Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Top;
		}
	}
}
