using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml.FrameworkElementTests
{
	[Sample("FrameworkElement", nameof(ItemsControl_Loaded))]
	public sealed partial class ItemsControl_Loaded : UserControl
	{
		public ItemsControl_Loaded()
		{
			this.InitializeComponent();
		}

		int loadedCount = 0;

		private void OnControlLoaded(object sender, object args)
		{
			loadedCount++;
			result.Text = $"Loaded: {loadedCount}";
		}
	}
}
