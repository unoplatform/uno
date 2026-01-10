using SamplesApp.Samples.NavigationViewSample;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.NavigationViewTests
{
	[Sample("NavigationView", nameof(NavigationView_TopNavigation))]
	public sealed partial class NavigationView_TopNavigation : UserControl
	{
		public NavigationView_TopNavigation()
		{
			this.InitializeComponent();
			contentFrame.Navigated += (s, e) =>
			{
				UpdateBackButton();
				Console.WriteLine($"Navigated to {e.Content}, {contentFrame.BackStackDepth}");

				if (e.NavigationMode == NavigationMode.Back)
				{
					NavigationViewItem getItemNamed(string name)
					=> nvSample.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == name);

					if (e.Content is Item1Page)
					{
						nvSample.SelectedItem = getItemNamed("SamplePage1");
					}
					else if (e.Content is Item2Page)
					{
						nvSample.SelectedItem = getItemNamed("SamplePage2");
					}
					else if (e.Content is Item3Page)
					{
						nvSample.SelectedItem = getItemNamed("SamplePage3");
					}
					else if (e.Content is SettingsPage)
					{
						nvSample.SelectedItem = nvSample.SettingsItem;
					}
				}
			};

			UpdateBackButton();
		}

		private void UpdateBackButton()
			=> nvSample.IsBackEnabled = contentFrame.BackStackDepth != 0;

		private void NavigationView_Loaded(object sender, RoutedEventArgs e)
		{
			Console.WriteLine("NavigationView_Loaded");
		}

		private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			Console.WriteLine("NavigationView_SelectionChanged");
		}

		private void NvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			if (args.IsSettingsInvoked)
			{
				contentFrame.Navigate(typeof(SettingsPage));
			}
			else if (args.InvokedItemContainer is NavigationViewItem item)
			{
				switch (item.Tag)
				{
					case "SamplePage1":
						contentFrame.Navigate(typeof(Item1Page));
						break;
					case "SamplePage2":
						contentFrame.Navigate(typeof(Item2Page));
						break;
					case "SamplePage3":
						contentFrame.Navigate(typeof(Item3Page));
						break;
				}
			}
		}

		private void NvSample_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
		{
			Console.WriteLine("NvSample_BackRequested");
			contentFrame.GoBack();
		}
	}
}
