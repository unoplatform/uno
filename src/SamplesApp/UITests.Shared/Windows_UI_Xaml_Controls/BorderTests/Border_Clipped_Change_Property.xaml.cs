using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.BorderTests
{
	[Sample("Border", description: "Border which is clipped by its parent. Tapping the border will change one of its properties, which shouldn't affect the clipping.")]
	public sealed partial class Border_Clipped_Change_Property : UserControl
	{
		public Border_Clipped_Change_Property()
		{
			this.InitializeComponent();
		}

		private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			var fe = sender as FrameworkElement;
			fe.ManipulationMode = ManipulationModes.None;
			(fe.Parent as FrameworkElement).ManipulationMode = ManipulationModes.None;
		}
	}
}
