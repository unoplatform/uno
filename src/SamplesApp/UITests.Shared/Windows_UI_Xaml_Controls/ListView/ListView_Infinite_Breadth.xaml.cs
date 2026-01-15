using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListView_Infinite_Breadth", ViewModelType = typeof(ListViewViewModel),
		Description = "Vertical ListView in a horizontal StackPanel (ie with infinite available width) with variable-width item text. Correct behavior is to resize as list scrolls.")]
	public sealed partial class ListView_Infinite_Breadth : UserControl
	{
		public ListView_Infinite_Breadth()
		{
			this.InitializeComponent();
		}
	}
}
