using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_Infinite_Breadth", typeof(ListViewViewModel),
		description: "Vertical ListView in a horizontal StackPanel (ie with infinite available width) with variable-width item text. Correct behavior is to resize as list scrolls.")]
	public sealed partial class ListView_Infinite_Breadth : UserControl
	{
		public ListView_Infinite_Breadth()
		{
			this.InitializeComponent();
		}
	}
}
