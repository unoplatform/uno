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
	[Sample("ListView", Name = "ListView_Fast_Scrolling", ViewModelType = typeof(ListViewViewModel),
		Description = "Used to test fast scrolling behavior in ListView. Ideally, the ListView should be able to keep up with fast scrolls and not freeze or stutter.")]
	public sealed partial class ListView_Fast_Scrolling : UserControl
	{
		public ListView_Fast_Scrolling()
		{
			this.InitializeComponent();
		}
	}
}
