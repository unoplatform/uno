using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	public sealed partial class DragDrop_ListViewMultiSelect : Page
	{
		public DragDrop_ListViewMultiSelect()
		{
			this.InitializeComponent();

			SUT.ItemsSource = new ObservableCollection<string>
			{
				"#FFFF0018",
				"#FFFFA52C",
				"#FFFFFF41",
				"#FF008018",
				"#FF0000F9",
				"#FF86007D",

				"#CCFF0018",
				"#CCFFA52C",
				"#CCFFFF41",
				"#CC008018",
				"#CC0000F9",
				"#CC86007D",

				"#66FF0018",
				"#66FFA52C",
				"#66FFFF41",
				"#66008018",
				"#660000F9",
				"#6686007D",

				"#33FF0018",
				"#33FFA52C",
				"#33FFFF41",
				"#33008018",
				"#330000F9",
				"#3386007D"
			};

			SUT.DragOver += (snd, e) =>
			{
				Operation.Text += e.AcceptedOperation.ToString();
			};
		}
	}
}
