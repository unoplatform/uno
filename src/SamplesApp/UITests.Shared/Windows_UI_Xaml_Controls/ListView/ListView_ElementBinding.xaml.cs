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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	/// <summary>
	/// 可用于自身或导航至 Frame 内部的空白页。
	/// </summary>

	[SampleControlInfoAttribute("ListView", "ListView_ElementBinding", description: "ListView with items that can be expanded using the toggle buttons.")]
	public sealed partial class ListView_ElementBinding : Page
	{
		public ListView_ElementBinding()
		{
			this.InitializeComponent();
			this.DataContext = "test113";
		}
	}
}
