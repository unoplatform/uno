using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Uno.UI.Common;
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

using ICommand = System.Windows.Input.ICommand;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.MenuBarTests
{
	[Sample("MenuBar", "SimpleMenuBar", description: "A simple MenuBar")]
	public sealed partial class SimpleMenuBar : UserControl
	{
		public SimpleMenuBar()
		{
			this.InitializeComponent();
		}

		public ICommand MyCommand => new DelegateCommand<object>(param => result.Text = $"command param:{param}");

		public ICommand MyXamlUICommand => new XamlUICommand
		{
			Label = "XamlCommand",
			Description = "My XamlCommand Description",
			Command = new DelegateCommand<object>(param => result.Text = $"xamluicommand param:{param}")
		};

		private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuFlyoutItem item)
			{
				result.Text = $"click text:{item.Text}";
			}
		}
	}
}
