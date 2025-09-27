﻿#pragma warning disable CS0105 // duplicate namespace because of WinUI source conversion

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
using Microsoft.UI.Xaml.Controls;

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

		private void OnItemInvoked(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
		{
			StatusTextBlock.Text = $"{DateTime.UtcNow.ToLongTimeString()}: {(args.InvokedItem as Microsoft.UI.Xaml.Controls.TreeViewNode)?.Content}";
		}
	}
}
