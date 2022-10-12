using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
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
