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

namespace Uno.UI.Samples.Content.UITests.Transform
{
	[SampleControlInfoAttribute("Transform", "Grid_With_RotateTransform_And_Button", typeof(nVentive.Umbrella.Presentation.Light.ViewModelBase), description:"Rotated Grid with Button inside. Button should be clickable.")]
	public sealed partial class Grid_With_RotateTransform_And_Button : UserControl
	{
		public Grid_With_RotateTransform_And_Button()
		{
			this.InitializeComponent();
		}

		private int counter;
		private void IncrementCounter(object sender, RoutedEventArgs e)
		{
			counter++;
			ClickCountTextBlock.Text = $"Button clicked {counter} times.";
		}
	}
}
