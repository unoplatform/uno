using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample("DragAndDrop", "ListView",
		Description = "This automated tests validate that items reordering in ListView is working properly.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class DragDrop_ListView : Page
	{
		public DragDrop_ListView()
		{
			this.InitializeComponent();

			SUT.ItemsSource = new ObservableCollection<string>
			{
				"#FF0018",
				"#FFA52C",
				"#FFFF41",
				"#008018",
				"#0000F9",
				"#86007D"
			};

			SUT.DragOver += (snd, e) =>
			{
				Operation.Text += e.AcceptedOperation.ToString();
			};
		}
	}
}
