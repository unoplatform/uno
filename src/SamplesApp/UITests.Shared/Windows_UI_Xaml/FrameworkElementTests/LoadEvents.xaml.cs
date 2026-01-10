using System;
using System.Linq;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Private.Infrastructure;

namespace UITests.Shared.Windows_UI_Xaml.FrameworkElementTests
{
	[Sample("FrameworkElement", "LoadEvents", Description = "Tests the Loaded/Unloaded events")]
	public sealed partial class LoadEvents : UserControl
	{
		public LoadEvents()
		{
			this.InitializeComponent();
		}

		private void OnLoadTextLoaded(object sender, RoutedEventArgs e)
		{
			if (sender is TextBlock block)
			{
				block.Text = "[OK] Loaded event received";
			}
		}

		private Panel _unloadTextParent;

		private async void OnUnloadTextLoaded(object sender, RoutedEventArgs e)
		{
			if (_unloadTextParent != null)
			{
				return;
			}

			var block = (TextBlock)sender;
			block.Text = "[PENDING] Loaded event received, try to unload it...";
			block.Loaded -= OnUnloadTextLoaded;

			_unloadTextParent = (Panel)block.Parent;

			await UnitTestDispatcherCompat.From(block).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => _unloadTextParent.Children.Remove(block));
		}

		private async void OnUnloadTextUnloaded(object sender, RoutedEventArgs e)
		{
			var block = (TextBlock)sender;
			block.Text = "[OK] Unloaded event received";
			block.Unloaded -= OnUnloadTextUnloaded;

			await UnitTestDispatcherCompat.From(block).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => _unloadTextParent.Children.Add(block));
		}
	}
}
