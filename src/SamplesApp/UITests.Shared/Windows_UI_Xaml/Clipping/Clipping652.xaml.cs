using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

using Uno.UI;

namespace UITests.Windows_UI_Xaml.Clipping
{
	[Sample("Clipping", "GH Bugs")]
	public sealed partial class Clipping652 : Page
	{
		public Clipping652()
		{
			this.InitializeComponent();

			DumpTree();
		}

		private async void DumpTree()
		{
#if UNO_REFERENCE_API
			await Task.Delay(1200);
			var tree = this.ShowLocalVisualTree();
			System.Diagnostics.Debug.WriteLine(tree);
#endif
		}
	}
}
