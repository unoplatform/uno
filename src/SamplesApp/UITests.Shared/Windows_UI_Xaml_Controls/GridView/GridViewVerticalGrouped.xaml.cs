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

namespace Uno.UI.Samples.Content.UITests.GridView
{
	[SampleControlInfoAttribute("GridView", "GridViewVerticalGrouped", typeof(GenericApp.Presentation.SamplePages.ListViewGroupedViewModel))]
	public sealed partial class GridViewVerticalGrouped : UserControl
	{
		public GridViewVerticalGrouped()
		{
			this.InitializeComponent();
		}
	}
}
