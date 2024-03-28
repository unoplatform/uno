// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

using ScrollPresenter = Windows.UI.Xaml.Controls.Primitives.ScrollPresenter;
using ScrollView = Windows.UI.Xaml.Controls.ScrollView;
using ScrollingScrollAnimationStartingEventArgs = Windows.UI.Xaml.Controls.ScrollingScrollAnimationStartingEventArgs;
using ScrollingZoomAnimationStartingEventArgs = Windows.UI.Xaml.Controls.ScrollingZoomAnimationStartingEventArgs;
using ScrollingScrollCompletedEventArgs = Windows.UI.Xaml.Controls.ScrollingScrollCompletedEventArgs;
using ScrollingZoomCompletedEventArgs = Windows.UI.Xaml.Controls.ScrollingZoomCompletedEventArgs;
//using MUXControlsTestHooks = Microsoft.UI.Private.Controls.MUXControlsTestHooks;
//using MUXControlsTestHooksLoggingMessageEventArgs = Microsoft.UI.Private.Controls.MUXControlsTestHooksLoggingMessageEventArgs;
using ScrollViewTestHooks = Microsoft.UI.Private.Controls.ScrollViewTestHooks;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

[Sample("Scrolling")]
public sealed partial class ScrollViewBlankPage : TestPage
{
	private object asyncEventReportingLock = new object();
	private List<string> lstAsyncEventMessage = new List<string>();

	public ScrollViewBlankPage()
	{
		this.InitializeComponent();

		//if (chkLogScrollViewMessages.IsChecked == true || chkLogScrollPresenterMessages.IsChecked == true)
		//{
		//    MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;

		//    if (chkLogScrollPresenterMessages.IsChecked == true)
		//    {
		//        MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//    }
		//    if (chkLogScrollViewMessages.IsChecked == true)
		//    {
		//        MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//    }
		//}
	}

	~ScrollViewBlankPage()
	{
	}

	//protected override void OnNavigatedFrom(NavigationEventArgs e)
	//{
	//    MUXControlsTestHooks.SetLoggingLevelForType("ScrollPresenter", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
	//    MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

	//    MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;

	//    base.OnNavigatedFrom(e);
	//}

	private void ChkCustomUI_Checked(object sender, RoutedEventArgs e)
	{
		if (grdCustomUI != null)
			grdCustomUI.Visibility = Visibility.Visible;
	}

	private void ChkCustomUI_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdCustomUI != null)
			grdCustomUI.Visibility = Visibility.Collapsed;
	}

	private void ChkLogs_Checked(object sender, RoutedEventArgs e)
	{
		if (grdLogs != null)
			grdLogs.Visibility = Visibility.Visible;
	}

	private void ChkLogs_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdLogs != null)
			grdLogs.Visibility = Visibility.Collapsed;
	}

	private void ScrollView_Loaded(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage($"ScrollView.Loaded");
		if (chkLogScrollPresenterEvents.IsChecked == true)
		{
			LogScrollPresenterInfo();
		}
		LogScrollViewInfo();
	}

	private void ScrollView_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		AppendAsyncEventMessage($"ScrollView.SizeChanged Size={scrollView.ActualWidth}, {scrollView.ActualHeight}");
		if (chkLogScrollPresenterEvents.IsChecked == true)
		{
			LogScrollPresenterInfo();
		}
		LogScrollViewInfo();
	}

	private void ScrollView_GettingFocus(UIElement sender, Windows.UI.Xaml.Input.GettingFocusEventArgs args)
	{
		FrameworkElement oldFE = args.OldFocusedElement as FrameworkElement;
		string oldFEName = (oldFE == null) ? "null" : oldFE.Name;
		FrameworkElement newFE = args.NewFocusedElement as FrameworkElement;
		string newFEName = (newFE == null) ? "null" : newFE.Name;

		AppendAsyncEventMessage($"ScrollView.GettingFocus FocusState={args.FocusState}, Direction={args.Direction}, InputDevice={args.InputDevice}, oldFE={oldFEName}, newFE={newFEName}");
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

		AppendAsyncEventMessage($"ScrollView.LosingFocus FocusState={args.FocusState}, Direction={args.Direction}, InputDevice={args.InputDevice}, oldFE={oldFEName}, newFE={newFEName}");
	}

	private void ScrollView_GotFocus(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage("ScrollView.GotFocus");
	}

	private void ScrollPresenter_Loaded(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage($"ScrollPresenter.Loaded");
		LogScrollPresenterInfo();
		if (chkLogScrollViewEvents.IsChecked == true)
		{
			LogScrollViewInfo();
		}
	}

	private void ScrollPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		AppendAsyncEventMessage($"ScrollPresenter.SizeChanged Size={scrollView.ActualWidth}, {scrollView.ActualHeight}");
		LogScrollPresenterInfo();
		if (chkLogScrollViewEvents.IsChecked == true)
		{
			LogScrollViewInfo();
		}
	}

	private void ScrollPresenter_ExtentChanged(ScrollPresenter sender, object args)
	{
		AppendAsyncEventMessage("ScrollPresenter.ExtentChanged ExtentWidth={sender.ExtentWidth}, ExtentHeight={sender.ExtentHeight}");
	}

	private void ScrollPresenter_StateChanged(ScrollPresenter sender, object args)
	{
		AppendAsyncEventMessage($"ScrollPresenter.StateChanged {sender.State.ToString()}");
	}

	private void ScrollPresenter_ViewChanged(ScrollPresenter sender, object args)
	{
		AppendAsyncEventMessage($"ScrollPresenter.ViewChanged HorizontalOffset={sender.HorizontalOffset.ToString()}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
	}

	private void ScrollPresenter_ScrollAnimationStarting(ScrollPresenter sender, ScrollingScrollAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollPresenter.ScrollAnimationStarting OffsetsChangeCorrelationId={args.CorrelationId}, SP=({args.StartPosition.X}, {args.StartPosition.Y}), EP=({args.EndPosition.X}, {args.EndPosition.Y})");
	}

	private void ScrollPresenter_ZoomAnimationStarting(ScrollPresenter sender, ScrollingZoomAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollPresenter.ZoomAnimationStarting ZoomFactorChangeCorrelationId={args.CorrelationId}, CenterPoint={args.CenterPoint}, SZF={args.StartZoomFactor}, EZF={args.EndZoomFactor}");
	}

	private void ScrollPresenter_ScrollCompleted(ScrollPresenter sender, ScrollingScrollCompletedEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollPresenter.ScrollCompleted OffsetsChangeCorrelationId={args.CorrelationId}");
	}

	private void ScrollPresenter_ZoomCompleted(ScrollPresenter sender, ScrollingZoomCompletedEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollPresenter.ZoomCompleted ZoomFactorChangeCorrelationId={args.CorrelationId}");
	}

	private void ScrollView_ExtentChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage($"ScrollView.ExtentChanged ExtentWidth={sender.ExtentWidth}, ExtentHeight={sender.ExtentHeight}");
	}

	private void ScrollView_StateChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage($"ScrollView.StateChanged {sender.State.ToString()}");
	}

	private void ScrollView_ViewChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage($"ScrollView.ViewChanged HorizontalOffset={sender.HorizontalOffset.ToString()}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
	}

	private void ScrollView_ScrollAnimationStarting(ScrollView sender, ScrollingScrollAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.ScrollAnimationStarting OffsetsChangeCorrelationId={args.CorrelationId}");
	}

	private void ScrollView_ZoomAnimationStarting(ScrollView sender, ScrollingZoomAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.ZoomAnimationStarting ZoomFactorChangeCorrelationId={args.CorrelationId}, CenterPoint={args.CenterPoint}");
	}

	private void ScrollView_ScrollCompleted(ScrollView sender, ScrollingScrollCompletedEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.ScrollCompleted OffsetsChangeCorrelationId={args.CorrelationId}");
	}

	private void ScrollView_ZoomCompleted(ScrollView sender, ScrollingZoomCompletedEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.ZoomCompleted ZoomFactorChangeCorrelationId={args.CorrelationId}");
	}

	private void LogScrollPresenterInfo()
	{
		ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

		AppendAsyncEventMessage($"ScrollPresenter Info: HorizontalOffset={scrollPresenter.HorizontalOffset}, VerticalOffset={scrollPresenter.VerticalOffset}, ZoomFactor={scrollPresenter.ZoomFactor}");
		AppendAsyncEventMessage($"ScrollPresenter Info: ViewportWidth={scrollPresenter.ViewportWidth}, ExtentHeight={scrollPresenter.ViewportHeight}");
		AppendAsyncEventMessage($"ScrollPresenter Info: ExtentWidth={scrollPresenter.ExtentWidth}, ExtentHeight={scrollPresenter.ExtentHeight}");
		AppendAsyncEventMessage($"ScrollPresenter Info: ScrollableWidth={scrollPresenter.ScrollableWidth}, ScrollableHeight={scrollPresenter.ScrollableHeight}");
	}

	private void LogScrollViewInfo()
	{
		AppendAsyncEventMessage($"ScrollView Info: HorizontalOffset={scrollView.HorizontalOffset}, VerticalOffset={scrollView.VerticalOffset}, ZoomFactor={scrollView.ZoomFactor}");
		AppendAsyncEventMessage($"ScrollView Info: ViewportWidth={scrollView.ViewportWidth}, ExtentHeight={scrollView.ViewportHeight}");
		AppendAsyncEventMessage($"ScrollView Info: ExtentWidth={scrollView.ExtentWidth}, ExtentHeight={scrollView.ExtentHeight}");
		AppendAsyncEventMessage($"ScrollView Info: ScrollableWidth={scrollView.ScrollableWidth}, ScrollableHeight={scrollView.ScrollableHeight}");
	}

	private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
	{
		lstLogs.Items.Clear();
	}

	private void ChkLogScrollPresenterEvents_Checked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null)
		{
			ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

			if (scrollPresenter != null)
			{
				scrollPresenter.Loaded += ScrollPresenter_Loaded;
				scrollPresenter.SizeChanged += ScrollPresenter_SizeChanged;
				scrollPresenter.ExtentChanged += ScrollPresenter_ExtentChanged;
				scrollPresenter.StateChanged += ScrollPresenter_StateChanged;
				scrollPresenter.ViewChanged += ScrollPresenter_ViewChanged;
				scrollPresenter.ScrollAnimationStarting += ScrollPresenter_ScrollAnimationStarting;
				scrollPresenter.ZoomAnimationStarting += ScrollPresenter_ZoomAnimationStarting;
				scrollPresenter.ScrollCompleted += ScrollPresenter_ScrollCompleted;
				scrollPresenter.ZoomCompleted += ScrollPresenter_ZoomCompleted;
			}
		}
	}

	private void ChkLogScrollPresenterEvents_Unchecked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null)
		{
			ScrollPresenter scrollPresenter = scrollView.ScrollPresenter;

			if (scrollPresenter != null)
			{
				scrollPresenter.Loaded -= ScrollPresenter_Loaded;
				scrollPresenter.SizeChanged -= ScrollPresenter_SizeChanged;
				scrollPresenter.ExtentChanged -= ScrollPresenter_ExtentChanged;
				scrollPresenter.StateChanged -= ScrollPresenter_StateChanged;
				scrollPresenter.ViewChanged -= ScrollPresenter_ViewChanged;
				scrollPresenter.ScrollAnimationStarting -= ScrollPresenter_ScrollAnimationStarting;
				scrollPresenter.ZoomAnimationStarting -= ScrollPresenter_ZoomAnimationStarting;
				scrollPresenter.ScrollCompleted -= ScrollPresenter_ScrollCompleted;
				scrollPresenter.ZoomCompleted -= ScrollPresenter_ZoomCompleted;
			}
		}
	}

	private void ChkLogScrollViewEvents_Checked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null)
		{
			scrollView.GettingFocus += ScrollView_GettingFocus;
			scrollView.GotFocus += ScrollView_GotFocus;
			scrollView.LosingFocus += ScrollView_LosingFocus;
			scrollView.LostFocus += ScrollView_LostFocus;
			scrollView.Loaded += ScrollView_Loaded;
			scrollView.SizeChanged += ScrollView_SizeChanged;
			scrollView.ExtentChanged += ScrollView_ExtentChanged;
			scrollView.StateChanged += ScrollView_StateChanged;
			scrollView.ViewChanged += ScrollView_ViewChanged;
			scrollView.ScrollAnimationStarting += ScrollView_ScrollAnimationStarting;
			scrollView.ZoomAnimationStarting += ScrollView_ZoomAnimationStarting;
			scrollView.ScrollCompleted += ScrollView_ScrollCompleted;
			scrollView.ZoomCompleted += ScrollView_ZoomCompleted;
		}
	}

	private void ChkLogScrollViewEvents_Unchecked(object sender, RoutedEventArgs e)
	{
		if (scrollView != null)
		{
			scrollView.GettingFocus -= ScrollView_GettingFocus;
			scrollView.GotFocus -= ScrollView_GotFocus;
			scrollView.LosingFocus -= ScrollView_LosingFocus;
			scrollView.LostFocus -= ScrollView_LostFocus;
			scrollView.Loaded -= ScrollView_Loaded;
			scrollView.SizeChanged -= ScrollView_SizeChanged;
			scrollView.ExtentChanged -= ScrollView_ExtentChanged;
			scrollView.StateChanged -= ScrollView_StateChanged;
			scrollView.ViewChanged -= ScrollView_ViewChanged;
			scrollView.ScrollAnimationStarting -= ScrollView_ScrollAnimationStarting;
			scrollView.ZoomAnimationStarting -= ScrollView_ZoomAnimationStarting;
			scrollView.ScrollCompleted -= ScrollView_ScrollCompleted;
			scrollView.ZoomCompleted -= ScrollView_ZoomCompleted;
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
	//    // Cut off the terminating new line.
	//    string msg = args.Message.Substring(0, args.Message.Length - 1);
	//    string asyncEventMessage = string.Empty;
	//    string senderName = string.Empty;

	//    try
	//    {
	//        FrameworkElement fe = sender as FrameworkElement;

	//        if (fe != null)
	//        {
	//            senderName = "s:" + fe.Name + ", ";
	//        }
	//    }
	//    catch
	//    {
	//    }

	//    if (args.IsVerboseLevel)
	//    {
	//        asyncEventMessage = "Verbose: " + senderName + "m:" + msg;
	//    }
	//    else
	//    {
	//        asyncEventMessage = "Info: " + senderName + "m:" + msg;
	//    }

	//    AppendAsyncEventMessage(asyncEventMessage);
	//}

	private void AppendAsyncEventMessage(string asyncEventMessage)
	{
		lock (asyncEventReportingLock)
		{
			while (asyncEventMessage.Length > 0)
			{
				string msgHead = asyncEventMessage;

				if (asyncEventMessage.Length > 110)
				{
					int commaIndex = asyncEventMessage.IndexOf(',', 110);
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

	private void BtnClearExceptionReport_Click(object sender, RoutedEventArgs e)
	{
		txtExceptionReport.Text = string.Empty;
	}
}
