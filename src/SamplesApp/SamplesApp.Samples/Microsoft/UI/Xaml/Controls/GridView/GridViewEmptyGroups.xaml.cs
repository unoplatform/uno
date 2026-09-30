using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
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
	[Sample("GridView", ViewModelType = typeof(ListViewGroupedViewModel))]
	public sealed partial class GridViewEmptyGroups : UserControl
	{
		public GridViewEmptyGroups()
		{
			this.InitializeComponent();
		}
	}
}
