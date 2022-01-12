using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample("DragAndDrop", "ListView",
		Description = "This automated tests validate that items reordering in ListView which has padding is working properly, including when dragging over that padding.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class DragDrop_ListView_WithPadding : Page
	{
		public DragDrop_ListView_WithPadding()
		{
			this.InitializeComponent();

			var source = new ObservableCollection<string>
			{
				"#FF0018",
				"#FFA52C",
				"#FFFF41",
				"#008018",
				"#0000F9",
				"#86007D"
			};

			source.CollectionChanged += (snd, e) => Dump();

			SUT.ItemsSource = source;
			Dump();

			void Dump()
				=> Result.Text = string.Join(";", source);
		}
	}
}
