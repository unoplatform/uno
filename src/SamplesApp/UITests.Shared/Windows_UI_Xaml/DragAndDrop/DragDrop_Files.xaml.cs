using System;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

#if HAS_UNO_WINUI || WINAPPSDK
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample(
		Description =
			"This test showcases the common scenario of dragging files and/or folders from another window (usually a file explorer) into a specific rectangle that accepts the drop. The files that were dragged and dropped should be seen as a list.",
		IsManualTest = true,
		IgnoreInSnapshotTests = true)]
	public sealed partial class DragDrop_Files : UserControl
	{
		public DragDrop_Files()
		{
			this.InitializeComponent();
		}

		private void OnDragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
		{
			e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems) ? DataPackageOperation.Copy : DataPackageOperation.None;

			e.DragUIOverride.IsCaptionVisible = false;
			e.DragUIOverride.IsGlyphVisible = false;
		}

		private async void OnDrop(object sender, Windows.UI.Xaml.DragEventArgs e)
		{
			if (e.DataView.Contains(StandardDataFormats.StorageItems))
			{
				var items = await e.DataView.GetStorageItemsAsync();
				lv.ItemsSource = items.Select(i => i.Path).ToList();
			}
		}
	}
}
