#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Windows.UI;
using System.Windows.Input;
using Windows.Foundation.Metadata;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.RadioMenuFlyoutItemTests
{
	[Sample("Flyouts")]
	public sealed partial class RadioMenuFlyoutItemPage : Page
	{
		Dictionary<string, TextBlock> itemStates;

		public RadioMenuFlyoutItemPage()
		{
			this.InitializeComponent();

			itemStates = new Dictionary<string, TextBlock>();

			if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Controls.MenuFlyoutItem, Uno.UI", "Icon"))
			{
				IconMenuFlyoutItem.Icon = new SymbolIcon(Symbol.Calendar);
				IconRadioMenuFlyoutItem.Icon = new SymbolIcon(Symbol.Calculator);
				IconRadioMenuFlyoutItem2.Icon = new SymbolIcon(Symbol.Calculator);
			}

			if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Controls.Grid, Uno.UI", "ColumnSpacing"))
			{
				ItemNames.Spacing = 4;
				ItemStates.Spacing = 4;
			}

			// register all RadioMenuFlyoutItems
			foreach (MenuFlyoutItemBase item in ButtonMenuFlyout.Items)
			{
				RegisterItem(item);
			}
			foreach (MenuFlyoutItemBase item in ButtonSubMenuFlyout.Items)
			{
				RegisterItem(item);
			}
			foreach (MenuFlyoutItemBase item in RadioSubMenu.Items)
			{
				RegisterItem(item);
			}
		}

		private void RegisterItem(MenuFlyoutItemBase item)
		{
			if (item is RadioMenuFlyoutItem)
			{
				RadioMenuFlyoutItem radioItem = item as RadioMenuFlyoutItem;

				radioItem.RegisterPropertyChangedCallback(RadioMenuFlyoutItem.IsCheckedProperty, new DependencyPropertyChangedCallback(IsCheckedChanged));

				TextBlock nameText = new TextBlock();
				nameText.Text = radioItem.Text;
				ItemNames.Children.Add(nameText);

				TextBlock stateText = new TextBlock();
				AutomationProperties.SetName(stateText, radioItem.Text + "State");
				stateText.Name = radioItem.Text + "State"; // Uno specific to allow _app.Query() in UI Tests.
				UpdateTextState(radioItem, stateText);
				ItemStates.Children.Add(stateText);

				itemStates.Add(radioItem.Text, stateText);
			}
		}

		private void IsCheckedChanged(DependencyObject o, DependencyProperty p)
		{
			RadioMenuFlyoutItem radioItem = o as RadioMenuFlyoutItem;
			TextBlock stateText;
			if (itemStates.TryGetValue(radioItem.Text, out stateText))
			{
				UpdateTextState(radioItem, stateText);
			}
		}

		private void UpdateTextState(RadioMenuFlyoutItem item, TextBlock textBlock)
		{
			textBlock.Text = item.IsChecked ? "Checked" : "Unchecked";
		}

	}
}
