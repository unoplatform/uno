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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI.Xaml_Automation
{
	[SampleControlInfo("Automation", nameof(AutomationProperties_AutomationId))]
	public sealed partial class AutomationProperties_AutomationId : UserControl
	{
		public AutomationProperties_AutomationId()
		{
			this.InitializeComponent();

			myList.ItemsSource = new[] {
				new MyItem { AutomationId = "Item01", Text = "Item 01" },
				new MyItem { AutomationId = "Item02", Text = "Item 02" },
				new MyItem { AutomationId = "Item03", Text = "Item 03" },
			};

			myList.SelectionChanged += (s, e) =>
			{
				if (myList.SelectedItem is MyItem item)
				{
					result.Text = item.Text;
				}
				else
				{
					result.Text = "Clicked item is not of type MyItem";
				}
			};
		}
	}

	[Bindable]
	public class MyItem
	{
		public string Text { get; set; }
		public string AutomationId { get; set; }
	}
}
