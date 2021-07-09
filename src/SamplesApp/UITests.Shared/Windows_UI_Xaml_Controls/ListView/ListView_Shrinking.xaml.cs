using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Windows.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_Shrinking", null, description: "Verify the ListView grows and shrinks with new items.")]
	public sealed partial class ListView_Shrinking : UserControl
    {
		public ObservableCollection<string> RandomItems { get; set; } = new ObservableCollection<string>();

        public ListView_Shrinking()
        {
            this.InitializeComponent();
			this.DataContext = this;
        }

		public void AddRandomItem(object sender, RoutedEventArgs e)
		{
			RandomItems.Add(Guid.NewGuid().ToString());
		}

		public void RemoveLastItem(object sender, RoutedEventArgs e)
		{
			RandomItems.Remove(RandomItems.Last());
		}

		public void RemoveItem(object sender, ItemClickEventArgs e)
		{
			RandomItems.Remove((string)e.ClickedItem);
		}
    }
}
