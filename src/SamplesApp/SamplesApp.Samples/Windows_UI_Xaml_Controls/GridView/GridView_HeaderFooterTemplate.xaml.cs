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

namespace Uno.UI.Samples.Content.UITests.GridView
{
	[Sample("GridView", ViewModelType = typeof(ListViewViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class GridView_HeaderFooterTemplate : UserControl
	{
		public GridView_HeaderFooterTemplate()
		{
			this.InitializeComponent();
		}
	}
}
