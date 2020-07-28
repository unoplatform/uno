using Uno.UI.Samples.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

#if NETFX_CORE
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#elif XAMARIN
using Windows.UI.Xaml.Controls;
using System.Globalization;
#endif

namespace Uno.UI.Samples.Content.UITests.GridView
{
	[SampleControlInfoAttribute("GridView", "GridView_Inside_ScrollViewer", typeof(GenericApp.Presentation.SamplePages.ListViewGroupedViewModel))]
	public sealed partial class GridView_Inside_ScrollViewer : UserControl
	{
		public GridView_Inside_ScrollViewer()
		{
			this.InitializeComponent();
		}
	}
}
