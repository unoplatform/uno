using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.Repeater
{
	[Sample("ItemsRepeater", IsManualTest = true, Description = "ItemsRepeater with NumberBox. User should be able to change the value of the different NumberBoxes.")]
	public sealed partial class ItemsRepeater_NumberBox : Page
	{
		public ItemsRepeater_NumberBox()
		{
			this.InitializeComponent();

			this.MainItemsRepeater.ItemsSource = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		}
	}
}
