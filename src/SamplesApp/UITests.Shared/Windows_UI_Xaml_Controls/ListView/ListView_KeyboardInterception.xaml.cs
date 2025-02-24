using System;
using System.Linq;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ListView;

[SampleControlInfo("ListView", "ListView_KeyboardInterception", Description = "Validate that we can add spaces and enter in TextBox that are in a ListView", IsManualTest = true)]
public sealed partial class ListView_KeyboardInterception : Page
{
	public ListView_KeyboardInterception()
	{
		this.InitializeComponent();
	}
}
