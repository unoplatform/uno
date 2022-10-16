using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

using Uno.UI;

namespace UITests.Windows_UI_Xaml.Clipping
{
	[Sample("Clipping")]
	public sealed partial class Clipping652 : Page
	{
		public Clipping652()
		{
			this.InitializeComponent();

#if UNO_REFERENCE_API
			DumpTree();

			async void DumpTree()
			{
				await Task.Delay(1200);
				var tree = this.ShowLocalVisualTree();
				System.Diagnostics.Debug.WriteLine(tree);
			}
#endif
		}


	}
}
