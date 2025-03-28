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

namespace Uno.UI.Samples.Content.UITests.TouchEventsTests
{
	[SampleControlInfo("Pointers", "TouchRotated", description: "Button inside a rotated Grid. The touch should still work.")]
	public sealed partial class TouchRotated : UserControl
	{
		public TouchRotated()
		{
			this.InitializeComponent();
		}

		private int _clickCount;
		private void OnButtonClicked(object sender, RoutedEventArgs e)
		{
			_clickCount++;
			ClickTextBlock.Text = $"Button clicked {_clickCount} times";
		}
	}
}
