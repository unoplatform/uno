using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.GridView
{
	[Sample("GridView", ViewModelType = typeof(ListViewViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class GridViewResizableLegacy : UserControl
	{
		public GridViewResizableLegacy()
		{
			this.InitializeComponent();
		}
	}
}
