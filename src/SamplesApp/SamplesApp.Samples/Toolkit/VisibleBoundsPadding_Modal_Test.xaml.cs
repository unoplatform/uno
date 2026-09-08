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

namespace UITests.Toolkit
{
	[Sample("Toolkit")]
	public partial class VisibleBoundsPadding_Modal_Test : Page
	{
		public VisibleBoundsPadding_Modal_Test()
		{
			this.InitializeComponent();
		}

		// The modal used to be presented through a native UIViewController on iOS. Uno elements are no
		// longer UIViews, so a window-sized Popup is what puts the page over the visible bounds now.
		private void LaunchModalSample(object sender, RoutedEventArgs e)
		{
			var modal = new VisibleBoundsPadding_Modal
			{
				Width = XamlRoot.Size.Width,
				Height = XamlRoot.Size.Height,
			};

			var popup = new Popup
			{
				XamlRoot = XamlRoot,
				ShouldConstrainToRootBounds = false,
				Child = modal,
			};

			popup.IsOpen = true;
		}
	}
}
