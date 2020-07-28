using Uno.UI.Samples.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.GridView
{
	[SampleControlInfoAttribute("GridView", "GridViewSelection", typeof(Presentation.SamplePages.GridView.GridViewWithSelectionViewModel))]
	public sealed partial class GridViewSelection : UserControl
	{
		public GridViewSelection()
		{
			this.InitializeComponent();			
		}
	}
}
