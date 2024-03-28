#if __WASM__
using SampleControl.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uno.Disposables;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;

namespace SampleControl.Presentation
{

	public partial class SampleChooserViewModel
	{
		public void PrintViewHierarchy(UIElement vg, StringBuilder sb, int level = 0)
		{

		}

		private async Task<StorageFolder> GetStorageFolderFromNameOrCreate(CancellationToken ct, string folderName)
		{
			var root = ApplicationData.Current.LocalFolder;
			var folder = await root.CreateFolderAsync(folderName,
				CreationCollisionOption.OpenIfExists
				).AsTask(ct);
			return folder;
		}
	}
}
#endif
