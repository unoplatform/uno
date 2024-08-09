// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using NavigationViewPaneDisplayMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
//using MaterialHelperTestApi = Microsoft.UI.Private.Media.MaterialHelperTestApi;
using NavigationView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView;
using NavigationViewItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewSelectionChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using Uno.UI.Samples.Controls;
using MUXControlsTestApp.Utilities;

namespace MUXControlsTestApp
{
	public class Customer
	{
		public String FirstName { get; set; }
		public String LastName { get; set; }
		public String Address { get; set; }

		public Customer(String firstName, String lastName, String address)
		{
			this.FirstName = firstName;
			this.LastName = lastName;
			this.Address = address;
		}
	}

	public class Customers : ObservableCollection<Customer>
	{
		public Customers()
		{
			Add(new Customer("Michael", "Anderberg",
					"Apartment 45"));
			Add(new Customer("Chris", "Ashton",
					"Apartment 67"));
			Add(new Customer("Seo-yun", "Jun",
					"Apartment 89"));
			Add(new Customer("Guido", "Pica",
					"Apartment 10"));
		}
	}

	[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewItemTemplatePage : TestPage
	{
		public NavigationViewItemTemplatePage()
		{
			this.InitializeComponent();
		}


		private void FlipOrientation_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneDisplayMode = NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? NavigationViewPaneDisplayMode.Auto : NavigationViewPaneDisplayMode.Top;
		}

		private void NavView_SelectionChanged(Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView sender, Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
		{
			var children = (Customer)args.SelectedItemContainer.Content;
			var customer = (Customer)args.SelectedItem;
			if (children != null && customer != null)
			{
				SelectionEventResult.Text = "Passed";
			}
			else
			{
				SelectionEventResult.Text = "Failed";
			}

			// Reset argument type indicatiors
			SelectionChangedItemType.Text = "null";
			SelectionChangedItemContainerType.Text = "null";

			// Update argument type indicators
			if (args.SelectedItem != null)
			{
				SelectionChangedItemType.Text = args.SelectedItem.GetType().ToString();
			}

			if (args.SelectedItemContainer != null)
			{
				SelectionChangedItemContainerType.Text = args.SelectedItemContainer.GetType().ToString();
			}
		}

		private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			// Reset argument type indicatiors
			ItemInvokedItemType.Text = "null";
			ItemInvokedItemContainerType.Text = "null";

			if (args.InvokedItem != null)
			{
				ItemInvokedItemType.Text = args.InvokedItem.GetType().ToString();
			}

			if (args.InvokedItemContainer != null)
			{
				ItemInvokedItemContainerType.Text = args.InvokedItemContainer.GetType().ToString();
			}
		}
	}
}
