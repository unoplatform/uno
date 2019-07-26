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

namespace Uno.UI.Samples.Content.UITests.Clip
{
	[SampleControlInfo("Clip", "ButtonWithClippingAndOffset", description: "Button with Clip set to offset rectangle. Touch should be detected correctly.")]
	public sealed partial class ButtonWithClippingAndOffset : UserControl
	{
		public ButtonWithClippingAndOffset()
		{
			this.InitializeComponent();
		}

		private int counter;
		private void IncrementCounter(object sender, RoutedEventArgs e)
		{
			counter++;
			ClickCountTextBlock.Text = $"Outer button clicked {counter} times.";
		}

		private int innerCounter;
		private void IncrementCounterInner(object sender, RoutedEventArgs e)
		{
			innerCounter++;
			InnerClickCountTextBlock.Text = $"Inner button clicked {innerCounter} times.";
		}
	}
}
