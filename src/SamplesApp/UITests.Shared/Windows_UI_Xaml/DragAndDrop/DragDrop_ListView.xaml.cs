using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample("DragAndDrop", "ListView")]
	public sealed partial class DragDrop_ListView : Page
	{
		public DragDrop_ListView()
		{
			this.InitializeComponent();

			Bla.ItemsSource = new ObservableCollection<string>
			{
				"#FF0018",
				"#FFA52C",
				"#FFFF41",
				"#008018",
				"#0000F9",
				"#86007D"
			};
		}
	}
}
