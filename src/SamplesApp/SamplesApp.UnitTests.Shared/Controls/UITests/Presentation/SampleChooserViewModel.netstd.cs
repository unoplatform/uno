#if __SKIA__
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

namespace SampleControl.Presentation
{

	public partial class SampleChooserViewModel
	{
		public void PrintViewHierarchy(UIElement vg, StringBuilder sb, int level = 0)
		{
			
		}

		private async Task DumpOutputFolderName(CancellationToken ct, string folderName)
		{
			var fullPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), folderName);

			Console.WriteLine($"Output folder for tests: {fullPath}");
		}

		private async Task GenerateBitmap(CancellationToken ct, string folderName, string fileName, IFrameworkElement content)
		{
			
		}
	}
}
#endif
