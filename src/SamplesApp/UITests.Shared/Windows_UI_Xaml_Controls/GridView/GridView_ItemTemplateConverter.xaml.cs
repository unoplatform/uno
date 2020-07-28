using nVentive.Umbrella.UI.Samples.Controls;
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
using nVentive.Umbrella.Views.UI.Xaml.Controls;
using System.Globalization;
#endif

namespace nVentive.Umbrella.Views.UI.Samples.Content.UITests.GridView
{
	[SampleControlInfoAttribute("GridView", "GridView_WithItemTemplateBinding", typeof(Presentation.SamplePages.GridView.GridViewViewModel))]
	public sealed partial class GridView_ItemTemplateConverter : UserControl
	{
		public GridView_ItemTemplateConverter()
		{
			this.InitializeComponent();
		}
	}
}
