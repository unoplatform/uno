using Uno;
using Uno.UI.Samples.Controls;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using System;
using System.Collections.Generic;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Description: "Demonstrates the Id Items Cache When ItemSource Length change")]
	public sealed partial class ListView_VirtualizePanelAdaptaterIdCache : UserControl
	{
		public List<int> LotsOfNumbers { get; } = Enumerable.Range(0, 500).ToList();

		public ListView_VirtualizePanelAdaptaterIdCache()
		{
			this.InitializeComponent();
			MyListView.ItemsSource = LotsOfNumbers;
			MyButton.Click += (sender, e) =>
			{
				UpdateItemSource();
			};
		}

		private void UpdateItemSource()
		{
			try
			{
				LotsOfNumbers.RemoveRange(10, 20);
				MyListView.ItemsSource = LotsOfNumbers;
				TextResult.Text = "Success";
			}
			catch (Exception ex)
			{
				TextResult.Text = "ItemSource Update Fails " + ex.Message;
			}
		}

	}
}
