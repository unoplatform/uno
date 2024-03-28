#pragma warning disable CS0105 // duplicate namespace because of WinUI source conversion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.TreeViewTests
{
	[Sample("TreeView")]
	public sealed partial class TreeView_ItemInvoked : Page
	{
		public TreeView_ItemInvoked()
		{
			this.InitializeComponent();
			Tree.ItemInvoked += OnItemInvoked;
		}

		private void OnItemInvoked(Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeView sender, Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
		{
			StatusTextBlock.Text = $"{DateTime.UtcNow.ToLongTimeString()}: {(args.InvokedItem as Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewNode)?.Content}";
		}
	}
}
