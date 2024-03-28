// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeControl_TestUI/SwipeControlPage2.xaml.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI;
using Uno.UI.Samples.Controls;

using IconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.IconSource;
using SwipeItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItem;
using SwipeItems = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItems;
using SwipeMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeMode;
using SwipeBehaviorOnInvoked = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeBehaviorOnInvoked;
using SwipeItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItemInvokedEventArgs;
//using MaterialHelperTestApi = Microsoft.UI.Private.Media.MaterialHelperTestApi;

namespace MUXControlsTestApp
{
	[Sample("SwipeControl")]
	public sealed partial class SwipeControlPage2 : Page //: TestPage
	{
		SwipeItem pastSender;
		public SwipeControlPage2()
		{
			this.InitializeComponent();
		}

		public void ChkSwipeControlProperties_Checked(object sender, RoutedEventArgs args)
		{
			if (svSwipeControlProperties != null)
			{
				svSwipeControlProperties.Visibility = Visibility.Visible;
			}
		}
		public void ChkSwipeControlProperties_Unchecked(object sender, RoutedEventArgs args)
		{
			if (svSwipeControlProperties != null)
			{
				svSwipeControlProperties.Visibility = Visibility.Collapsed;
			}
		}

		public void ChkSwipeMethods_Checked(object sender, RoutedEventArgs args)
		{
			if (svSwipeControlMethods != null)
			{
				svSwipeControlMethods.Visibility = Visibility.Visible;
			}
		}

		public void ChkSwipeMethods_Unchecked(object sender, RoutedEventArgs args)
		{
			if (svSwipeControlMethods != null)
			{
				svSwipeControlMethods.Visibility = Visibility.Collapsed;
			}
		}
		public void ChkSwipeItemProperties_Checked(object sender, RoutedEventArgs args)
		{
			if (svSwipeItemProperties != null)
			{
				svSwipeItemProperties.Visibility = Visibility.Visible;
			}
		}
		public void ChkSwipeItemProperties_Unchecked(object sender, RoutedEventArgs args)
		{
			if (svSwipeItemProperties != null)
			{
				svSwipeItemProperties.Visibility = Visibility.Collapsed;
			}
		}

		public void ChkSwipeEvents_Checked(object sender, RoutedEventArgs args)
		{
		}

		public void ChkSwipeEvents_Unchecked(object sender, RoutedEventArgs args)
		{
		}

		public void ChkPageMethods_Checked(object sender, RoutedEventArgs args)
		{
		}
		public void ChkPageMethods_Unchecked(object sender, RoutedEventArgs args)
		{
		}

		public void ChkSwipeTestHooks_Checked(object sender, RoutedEventArgs args)
		{
		}

		public void ChkSwipeTestHooks_Unchecked(object sender, RoutedEventArgs args)
		{
		}


		public void BtnGetLeftItems_Click(object sender, RoutedEventArgs args)
		{
			if (markupSwipeControl.LeftItems != null)
			{
				tblLeftItems.Text = markupSwipeControl.LeftItems.Count.ToString();
				tblLeftItems.Text += " - ";
				tblLeftItems.Text += markupSwipeControl.LeftItems.Mode.ToString();
			}
			else
			{
				tblLeftItems.Text = "0";
			}
		}
		public void BtnGetRightItems_Click(object sender, RoutedEventArgs args)
		{
			if (markupSwipeControl.RightItems != null)
			{
				tblRightItems.Text = markupSwipeControl.RightItems.Count.ToString();
				tblRightItems.Text += " - ";
				tblRightItems.Text += markupSwipeControl.RightItems.Mode.ToString();
			}
			else
			{
				tblRightItems.Text = "0";
			}
		}
		public void BtnGetTopItems_Click(object sender, RoutedEventArgs args)
		{
			if (markupSwipeControl.TopItems != null)
			{
				tblTopItems.Text = markupSwipeControl.TopItems.Count.ToString();
				tblTopItems.Text += " - ";
				tblTopItems.Text += markupSwipeControl.TopItems.Mode.ToString();
			}
			else
			{
				tblTopItems.Text = "0";
			}
		}
		public void BtnGetBottomItems_Click(object sender, RoutedEventArgs args)
		{
			if (markupSwipeControl.BottomItems != null)
			{
				tblBottomItems.Text = markupSwipeControl.BottomItems.Count.ToString();
				tblBottomItems.Text += " - ";
				tblBottomItems.Text += markupSwipeControl.BottomItems.Mode.ToString();
			}
			else
			{
				tblBottomItems.Text = "0";
			}
		}

		public void BtnClose_Click(object sender, RoutedEventArgs args)
		{
			markupSwipeControl.Close();
		}

		public void BtnAddItem_Click(object sender, RoutedEventArgs args)
		{
			SwipeItem swipeItem = new SwipeItem();
			swipeItem.Text = txtText.Text;
			switch (cmbIcon.SelectedIndex)
			{
				case (0):
					swipeItem.IconSource = Resources["SymbolIcon"] as IconSource;
					break;
				case (1):
					swipeItem.IconSource = Resources["FontIcon"] as IconSource;
					break;
				case (2):
					swipeItem.IconSource = Resources["BitmapIcon"] as IconSource;
					break;
				case (3):
					swipeItem.IconSource = Resources["PathIcon"] as IconSource;
					break;
			}
			swipeItem.Background = new SolidColorBrush(cpBackground.Color);
			switch (cmbBehaviorOnInvoked.SelectedIndex)
			{
				case (0):
					swipeItem.BehaviorOnInvoked = SwipeBehaviorOnInvoked.Auto;
					break;
				case (1):
					swipeItem.BehaviorOnInvoked = SwipeBehaviorOnInvoked.Close;
					break;
				case (2):
					swipeItem.BehaviorOnInvoked = SwipeBehaviorOnInvoked.RemainOpen;
					break;
			}
			SwipeItems swipeItems = null;
			switch (cmbCollection.SelectedIndex)
			{
				case (0):
					swipeItems = markupSwipeControl.LeftItems;
					if (swipeItems == null)
					{
						swipeItems = new SwipeItems();
						swipeItems.Add(swipeItem);
						markupSwipeControl.LeftItems = swipeItems;
					}
					else
					{
						swipeItems.Add(swipeItem);
					}
					break;
				case (1):
					swipeItems = markupSwipeControl.RightItems;
					if (swipeItems == null)
					{
						swipeItems = new SwipeItems();
						swipeItems.Add(swipeItem);
						markupSwipeControl.RightItems = swipeItems;
					}
					else
					{
						swipeItems.Add(swipeItem);
					}
					break;
				case (2):
					swipeItems = markupSwipeControl.TopItems;
					if (swipeItems == null)
					{
						swipeItems = new SwipeItems();
						swipeItems.Add(swipeItem);
						markupSwipeControl.TopItems = swipeItems;
					}
					else
					{
						swipeItems.Add(swipeItem);
					}
					break;
				case (3):
					swipeItems = markupSwipeControl.BottomItems;
					if (swipeItems == null)
					{
						swipeItems = new SwipeItems();
						swipeItems.Add(swipeItem);
						markupSwipeControl.BottomItems = swipeItems;
					}
					else
					{
						swipeItems.Add(swipeItem);
					}
					break;
			}
			switch (cmbMode.SelectedIndex)
			{
				case (0):
					swipeItems.Mode = SwipeMode.Reveal;
					break;
				case (1):
					swipeItems.Mode = SwipeMode.Execute;
					break;
			}
			resetSwipeItemChoices();
		}
		public void BtnCancel_Click(object sender, RoutedEventArgs args)
		{
			resetSwipeItemChoices();
		}

		private void resetSwipeItemChoices()
		{
			cmbCollection.SelectedIndex = 0;
			cmbMode.SelectedIndex = 0;
			txtText.Text = "";
			cmbIcon.SelectedIndex = 0;
			cpBackground.Color = Colors.White;
			cmbBehaviorOnInvoked.SelectedIndex = 0;
		}

		private void SwipeItemInvoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
		{
			// ensures that this method is invoked twice for only one swipe action.
			if (pastSender == sender)
			{
				textBlock.Text = "FailTest";
			}
			else
			{
				textBlock.Text = sender.Text;
			}
			pastSender = sender;

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(this);
			peer.RaiseAutomationEvent(AutomationEvents.MenuClosed);
		}
	}
}
