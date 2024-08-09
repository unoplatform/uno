// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Private.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

[Sample("Scrolling")]
public sealed partial class ScrollViewKeyboardAndGamepadNavigationPage : TestPage
{
	private Object asyncEventReportingLock = new Object();
	private List<string> lstAsyncEventMessage = new List<string>();

	public ScrollViewKeyboardAndGamepadNavigationPage()
	{
		this.InitializeComponent();
	}

	~ScrollViewKeyboardAndGamepadNavigationPage()
	{
	}

	//protected override void OnNavigatedFrom(NavigationEventArgs e)
	//{
	//	MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
	//	MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

	//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;

	//	base.OnNavigatedFrom(e);
	//}

	private void ScrollPresenter_ExtentChanged(ScrollPresenter sender, object args)
	{
		AppendAsyncEventMessage("ScrollPresenter.ExtentChanged ExtentWidth=" + sender.ExtentWidth + ", ExtentHeight=" + sender.ExtentHeight);
	}

	private void ScrollPresenter_StateChanged(ScrollPresenter sender, object args)
	{
		AppendAsyncEventMessage("ScrollPresenter.StateChanged " + sender.State.ToString());
	}

	private void ScrollPresenter_ViewChanged(ScrollPresenter sender, object args)
	{
		AppendAsyncEventMessage("ScrollPresenter.ViewChanged H=" + sender.HorizontalOffset.ToString() + ", V=" + sender.VerticalOffset + ", ZF=" + sender.ZoomFactor);
	}

	private void ScrollPresenter_ScrollAnimationStarting(ScrollPresenter sender, ScrollingScrollAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage("ScrollPresenter.ScrollAnimationStarting OffsetsChangeCorrelationId=" + args.CorrelationId + " SP=(" + args.StartPosition.X + "," + args.StartPosition.Y + ") EP=(" + args.EndPosition.X + "," + args.EndPosition.Y + ")");
	}

	private void ScrollPresenter_ZoomAnimationStarting(ScrollPresenter sender, ScrollingZoomAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage("ScrollPresenter.ZoomAnimationStarting ZoomFactorChangeCorrelationId=" + args.CorrelationId + ", CenterPoint=" + args.CenterPoint + ", SZF=" + args.StartZoomFactor + ", EZF=" + args.EndZoomFactor);
	}

	private void ScrollView_GettingFocus(UIElement sender, Windows.UI.Xaml.Input.GettingFocusEventArgs args)
	{
		FrameworkElement oldFE = args.OldFocusedElement as FrameworkElement;
		string oldFEName = (oldFE == null) ? "null" : oldFE.Name;
		FrameworkElement newFE = args.NewFocusedElement as FrameworkElement;
		string newFEName = (newFE == null) ? "null" : newFE.Name;

		AppendAsyncEventMessage("ScrollView.GettingFocus FocusState=" + args.FocusState + ", Direction=" + args.Direction + ", InputDevice=" + args.InputDevice + ", oldFE=" + oldFEName + ", newFE=" + newFEName);
	}

	private void ScrollView_LostFocus(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage("ScrollView.LostFocus");
	}

	private void ScrollView_LosingFocus(UIElement sender, Windows.UI.Xaml.Input.LosingFocusEventArgs args)
	{
		FrameworkElement oldFE = args.OldFocusedElement as FrameworkElement;
		string oldFEName = (oldFE == null) ? "null" : oldFE.Name;
		FrameworkElement newFE = args.NewFocusedElement as FrameworkElement;
		string newFEName = (newFE == null) ? "null" : newFE.Name;

		AppendAsyncEventMessage("ScrollView.LosingFocus FocusState=" + args.FocusState + ", Direction=" + args.Direction + ", InputDevice=" + args.InputDevice + ", oldFE=" + oldFEName + ", newFE=" + newFEName);
	}

	private void ScrollView_GotFocus(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage("ScrollView.GotFocus");
	}

	private void ScrollView_ExtentChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage("ScrollView.ExtentChanged ExtentWidth=" + sender.ExtentWidth + ", ExtentHeight=" + sender.ExtentHeight);
	}

	private void ScrollView_StateChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage("ScrollView.StateChanged " + sender.State.ToString());
	}

	private void ScrollView_ViewChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage("ScrollView.ViewChanged H=" + sender.HorizontalOffset.ToString() + ", V=" + sender.VerticalOffset + ", ZF=" + sender.ZoomFactor);
	}

	private void ScrollView_ScrollAnimationStarting(ScrollView sender, ScrollingScrollAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage("ScrollView.ScrollAnimationStarting OffsetsChangeCorrelationId=" + args.CorrelationId);
	}

	private void ScrollView_ZoomAnimationStarting(ScrollView sender, ScrollingZoomAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage("ScrollView.ZoomAnimationStarting ZoomFactorChangeCorrelationId=" + args.CorrelationId + ", CenterPoint=" + args.CenterPoint);
	}

	private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
	{
		lstLogs.Items.Clear();
	}

	private void ChkLogScrollPresenterEvents_Checked(object sender, RoutedEventArgs e)
	{
		if (muxScrollView != null)
		{
			ScrollPresenter scrollPresenter = muxScrollView.ScrollPresenter;

			if (scrollPresenter != null)
			{
				scrollPresenter.ExtentChanged += ScrollPresenter_ExtentChanged;
				scrollPresenter.StateChanged += ScrollPresenter_StateChanged;
				scrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
				scrollPresenter.ScrollAnimationStarting += ScrollPresenter_ScrollAnimationStarting;
				scrollPresenter.ZoomAnimationStarting += ScrollPresenter_ZoomAnimationStarting;
			}
		}
	}

	private void ChkLogScrollPresenterEvents_Unchecked(object sender, RoutedEventArgs e)
	{
		if (muxScrollView != null)
		{
			ScrollPresenter scrollPresenter = muxScrollView.ScrollPresenter;

			if (scrollPresenter != null)
			{
				scrollPresenter.ExtentChanged -= ScrollPresenter_ExtentChanged;
				scrollPresenter.StateChanged -= ScrollPresenter_StateChanged;
				scrollPresenter.ViewChanged -= ScrollPresenter_ViewChanged;
				scrollPresenter.ScrollAnimationStarting -= ScrollPresenter_ScrollAnimationStarting;
				scrollPresenter.ZoomAnimationStarting -= ScrollPresenter_ZoomAnimationStarting;
			}
		}
	}

	private void ChkLogScrollViewEvents_Checked(object sender, RoutedEventArgs e)
	{
		if (muxScrollView != null)
		{
			muxScrollView.GettingFocus += ScrollView_GettingFocus;
			muxScrollView.GotFocus += ScrollView_GotFocus;
			muxScrollView.LosingFocus += ScrollView_LosingFocus;
			muxScrollView.LostFocus += ScrollView_LostFocus;
			muxScrollView.ExtentChanged += ScrollView_ExtentChanged;
			muxScrollView.StateChanged += ScrollView_StateChanged;
			muxScrollView.ViewChanged += ScrollView_ViewChanged;
			muxScrollView.ScrollAnimationStarting += ScrollView_ScrollAnimationStarting;
			muxScrollView.ZoomAnimationStarting += ScrollView_ZoomAnimationStarting;
		}
	}

	private void ChkLogScrollViewEvents_Unchecked(object sender, RoutedEventArgs e)
	{
		if (muxScrollView != null)
		{
			muxScrollView.GettingFocus -= ScrollView_GettingFocus;
			muxScrollView.GotFocus -= ScrollView_GotFocus;
			muxScrollView.LosingFocus -= ScrollView_LosingFocus;
			muxScrollView.LostFocus -= ScrollView_LostFocus;
			muxScrollView.ExtentChanged -= ScrollView_ExtentChanged;
			muxScrollView.StateChanged -= ScrollView_StateChanged;
			muxScrollView.ViewChanged -= ScrollView_ViewChanged;
			muxScrollView.ScrollAnimationStarting -= ScrollView_ScrollAnimationStarting;
			muxScrollView.ZoomAnimationStarting -= ScrollView_ZoomAnimationStarting;
		}
	}

	private void ChkLogScrollPresenterMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogScrollViewMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollPresenterMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogScrollViewMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogScrollPresenterMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogScrollPresenterMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	//private void MUXControlsTestHooks_LoggingMessage(object sender, MUXControlsTestHooksLoggingMessageEventArgs args)
	//{
	//	if ((chkLogScrollPresenterMessages.IsChecked == false && sender is ScrollPresenter) ||
	//		(chkLogScrollViewMessages.IsChecked == false && sender is ScrollView))
	//	{
	//		return;
	//	}

	//	// Cut off the terminating new line.
	//	string msg = args.Message.Substring(0, args.Message.Length - 1);
	//	string asyncEventMessage = string.Empty;
	//	string senderName = string.Empty;

	//	FrameworkElement fe = sender as FrameworkElement;

	//	if (fe != null)
	//	{
	//		senderName = "s:" + fe.Name + ", ";
	//	}

	//	if (args.IsVerboseLevel)
	//	{
	//		asyncEventMessage = "Verbose: " + senderName + "m:" + msg;
	//	}
	//	else
	//	{
	//		asyncEventMessage = "Info: " + senderName + "m:" + msg;
	//	}

	//	AppendAsyncEventMessage(asyncEventMessage);
	//}

	private void AppendAsyncEventMessage(string asyncEventMessage)
	{
		lock (asyncEventReportingLock)
		{
			while (asyncEventMessage.Length > 0)
			{
				string msgHead = asyncEventMessage;

				if (asyncEventMessage.Length > 100)
				{
					int commaIndex = asyncEventMessage.IndexOf(',', 100);
					if (commaIndex != -1)
					{
						msgHead = asyncEventMessage.Substring(0, commaIndex);
						asyncEventMessage = asyncEventMessage.Substring(commaIndex + 1);
					}
					else
					{
						asyncEventMessage = string.Empty;
					}
				}
				else
				{
					asyncEventMessage = string.Empty;
				}

				lstAsyncEventMessage.Add(msgHead);
			}
			var ignored = this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, AppendAsyncEventMessage);

		}
	}

	private void AppendAsyncEventMessage()
	{
		lock (asyncEventReportingLock)
		{
			foreach (string asyncEventMessage in lstAsyncEventMessage)
			{
				lstLogs.Items.Add(asyncEventMessage);
			}
			lstAsyncEventMessage.Clear();
		}
	}
}
