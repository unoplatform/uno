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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.Canvas
{
	[SampleControlInfo(description: "The top blue Rectangle should be clipped by the Clip on the outer Border. The bottom one shouldn't be clipped.")]
	public sealed partial class Canvas_With_Outer_Clip : UserControl
	{
		public Canvas_With_Outer_Clip()
		{
			this.InitializeComponent();
		}
	}
}
