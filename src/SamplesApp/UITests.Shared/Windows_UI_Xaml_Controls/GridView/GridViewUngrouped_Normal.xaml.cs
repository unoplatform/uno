using Uno.UI.Samples.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace GenericApp.Views.Content.UITests.GridView
{
	[Sample("GridView", ViewModelType = typeof(ListViewGroupedViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class GridViewUngrouped_Normal : UserControl
	{
		public GridViewUngrouped_Normal()
		{
			this.InitializeComponent();
		}
	}
}
